using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ToolPool.Models;

namespace ToolPool.Services;

public class SendbirdException : Exception
{
    public HttpStatusCode Status { get; }
    public string ResponseBody { get; }

    public SendbirdException(string message, HttpStatusCode status, string responseBody)
        : base(message)
    {
        Status = status;
        ResponseBody = responseBody;
    }
}

public class SendbirdService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SendbirdService> _logger;
    private readonly SendbirdOptions _opt;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    private HttpClient client;
    private string BaseUrl => $"https://api-{_opt.AppId}.sendbird.com/v3";

    public SendbirdService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<SendbirdService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _opt = config.GetSection("Sendbird").Get<SendbirdOptions>() ?? new SendbirdOptions();
        client = _httpClientFactory.CreateClient();
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string path, object? body = null)
    {
        var req = new HttpRequestMessage(method, $"{BaseUrl}{path}");
        req.Headers.Add("Api-Token", _opt.ApiToken);
        if (body is not null)
            req.Content = JsonContent.Create(body);
        return req;
    }

    /// <summary>
    /// Ensures a Sendbird user exists for the given id. Strict: throws SendbirdException
    /// if the user cannot be created or fetched. Race-safe: a failed POST that collides
    /// with a concurrently-created user will be resolved by a final GET.
    /// </summary>
    public async Task<string> CreateOrGetUserAsync(string userId, string nickname)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("userId is required", nameof(userId));

        var escapedId = Uri.EscapeDataString(userId);

        using (var getReq = BuildRequest(HttpMethod.Get, $"/users/{escapedId}"))
        using (var getResp = await client.SendAsync(getReq))
        {
            if (getResp.IsSuccessStatusCode)
                return userId;

            var body = await SafeReadBody(getResp);
            if (!IsMissingUserResponse(getResp.StatusCode, body))
            {
                _logger.LogError("Sendbird GET /users/{UserId} failed unexpectedly: {Status} {Body}",
                    userId, (int)getResp.StatusCode, body);
                throw new SendbirdException(
                    $"Sendbird GET /users/{userId} failed: {(int)getResp.StatusCode}",
                    getResp.StatusCode, body);
            }
        }

        using (var createReq = BuildRequest(HttpMethod.Post, "/users", new
        {
            user_id = userId,
            nickname = string.IsNullOrWhiteSpace(nickname) ? userId : nickname,
            profile_url = BuildDefaultProfileUrl(userId, nickname)
        }))
        using (var createResp = await client.SendAsync(createReq))
        {
            if (createResp.IsSuccessStatusCode)
                return userId;

            var createBody = await SafeReadBody(createResp);

            // Race-safe fallback: a concurrent request may have just created this user.
            using var recheckReq = BuildRequest(HttpMethod.Get, $"/users/{escapedId}");
            using var recheckResp = await client.SendAsync(recheckReq);
            if (recheckResp.IsSuccessStatusCode)
            {
                _logger.LogInformation("Sendbird POST /users failed but user now exists (race): {UserId}", userId);
                return userId;
            }

            var recheckBody = await SafeReadBody(recheckResp);
            _logger.LogError(
                "Sendbird user provisioning failed for {UserId}. POST status={PostStatus} body={PostBody}; follow-up GET status={GetStatus} body={GetBody}",
                userId, (int)createResp.StatusCode, createBody,
                (int)recheckResp.StatusCode, recheckBody);
            throw new SendbirdException(
                $"Sendbird user provisioning failed for {userId}",
                createResp.StatusCode, createBody);
        }
    }

    /// <summary>
    /// Creates a fresh group channel for a specific rental conversation.
    /// Non-distinct on purpose: uniqueness per rental is enforced at the app layer
    /// via the Interest_Submissions row. Verifies membership before returning.
    /// </summary>
    public async Task<SendbirdChannel> CreateGroupChannelAsync(
        string renterId, string ownerId, string itemName, string? interestId = null)
    {
        await CreateOrGetUserAsync(renterId, renterId);
        await CreateOrGetUserAsync(ownerId, ownerId);

        var body = new Dictionary<string, object?>
        {
            ["user_ids"] = new[] { renterId, ownerId },
            ["is_distinct"] = false,
            ["name"] = $"Chat: {itemName}",
            ["custom_type"] = "item_chat"
        };
        if (!string.IsNullOrEmpty(interestId))
            body["data"] = interestId;

        SendbirdChannel channel;
        using (var req = BuildRequest(HttpMethod.Post, "/group_channels", body))
        using (var resp = await client.SendAsync(req))
        {
            if (!resp.IsSuccessStatusCode)
            {
                var errBody = await SafeReadBody(resp);
                _logger.LogError("Sendbird POST /group_channels failed: {Status} {Body} (renter={Renter} owner={Owner} tool={Tool})",
                    (int)resp.StatusCode, errBody, renterId, ownerId, itemName);
                throw new SendbirdException(
                    "Sendbird channel creation failed",
                    resp.StatusCode, errBody);
            }

            channel = await resp.Content.ReadFromJsonAsync<SendbirdChannel>(_json)
                ?? throw new SendbirdException("Sendbird channel response was empty", resp.StatusCode, "");
        }

        if (!HasBothMembers(channel, renterId, ownerId))
        {
            var refreshed = await FetchChannelWithMembersAsync(channel.ChannelUrl);
            if (refreshed is not null)
                channel = refreshed;
        }

        if (!HasBothMembers(channel, renterId, ownerId))
        {
            _logger.LogError("Sendbird channel {Url} missing expected members. Have: {Members}. Expected: {Renter}, {Owner}",
                channel.ChannelUrl,
                string.Join(",", channel.Members.Select(m => m.UserId)),
                renterId, ownerId);
            throw new SendbirdException(
                $"Sendbird channel {channel.ChannelUrl} missing expected members",
                HttpStatusCode.OK, "");
        }

        return channel;
    }

    /// <summary>
    /// Returns true iff the channel exists and both expected users are members.
    /// 404 → false. Other non-success → throws (treat as Sendbird outage, don't
    /// silently invalidate an otherwise-good channel URL).
    /// </summary>
    public async Task<bool> VerifyChannelMembersAsync(string channelUrl, string renterId, string ownerId)
    {
        if (string.IsNullOrWhiteSpace(channelUrl)) return false;

        var channel = await FetchChannelWithMembersAsync(channelUrl);
        if (channel is null) return false;
        return HasBothMembers(channel, renterId, ownerId);
    }

    private async Task<SendbirdChannel?> FetchChannelWithMembersAsync(string channelUrl)
    {
        var escaped = Uri.EscapeDataString(channelUrl);
        using var req = BuildRequest(HttpMethod.Get, $"/group_channels/{escaped}?show_member=true");
        using var resp = await client.SendAsync(req);

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!resp.IsSuccessStatusCode)
        {
            var body = await SafeReadBody(resp);
            _logger.LogError("Sendbird GET /group_channels/{Url} failed: {Status} {Body}",
                channelUrl, (int)resp.StatusCode, body);
            throw new SendbirdException(
                $"Sendbird GET group_channels failed: {(int)resp.StatusCode}",
                resp.StatusCode, body);
        }

        return await resp.Content.ReadFromJsonAsync<SendbirdChannel>(_json);
    }

    private static bool HasBothMembers(SendbirdChannel channel, string renterId, string ownerId)
    {
        if (channel.Members is null || channel.Members.Count == 0) return false;
        var ids = channel.Members.Select(m => m.UserId).ToHashSet(StringComparer.Ordinal);
        return ids.Contains(renterId) && ids.Contains(ownerId);
    }

    private static async Task<string> SafeReadBody(HttpResponseMessage resp)
    {
        try
        {
            var body = await resp.Content.ReadAsStringAsync();
            return body.Length > 512 ? body.Substring(0, 512) + "…" : body;
        }
        catch { return ""; }
    }

    private static string BuildDefaultProfileUrl(string userId, string? nickname)
    {
        var seed = string.IsNullOrWhiteSpace(nickname) ? userId : nickname;
        return $"https://api.dicebear.com/9.x/initials/svg?seed={Uri.EscapeDataString(seed)}";
    }

    private static bool IsMissingUserResponse(HttpStatusCode status, string body)
    {
        if (status == HttpStatusCode.NotFound)
            return true;

        try
        {
            var error = JsonSerializer.Deserialize<SendbirdErrorResponse>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return error?.Code == 400201;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Lists group channels the given user belongs to.
    /// </summary>
    public async Task<List<SendbirdChannel>> ListUserChannelsAsync(string userId)
    {
        var escapedId = Uri.EscapeDataString(userId);
        using var req = BuildRequest(HttpMethod.Get,
            $"/users/{escapedId}/my_group_channels?limit=20&order=latest_last_message");
        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var wrapper = await resp.Content.ReadFromJsonAsync<ChannelListResponse>(_json);
        return wrapper?.Channels ?? new List<SendbirdChannel>();
    }

    public string GetAppId() => _opt.AppId;
}

public class SendbirdChannel
{
    [JsonPropertyName("channel_url")]
    public string ChannelUrl { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("member_count")]
    public int MemberCount { get; set; }

    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }

    [JsonPropertyName("members")]
    public List<SendbirdMember> Members { get; set; } = new();

    [JsonPropertyName("data")]
    public string? Data { get; set; }
}

public class SendbirdMember
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = "";
}

public class ChannelListResponse
{
    [JsonPropertyName("channels")]
    public List<SendbirdChannel> Channels { get; set; } = new();
}

public class SendbirdErrorResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
