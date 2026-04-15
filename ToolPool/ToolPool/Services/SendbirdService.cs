using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ToolPool.Models;

namespace ToolPool.Services;

public class SendbirdService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SendbirdOptions _opt;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    private string BaseUrl => $"https://api-{_opt.AppId}.sendbird.com/v3";

    public SendbirdService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _opt = config.GetSection("Sendbird").Get<SendbirdOptions>() ?? new SendbirdOptions();
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
    /// Creates a Sendbird user if one doesn't already exist. Returns the user_id.
    /// </summary>
    public async Task<string> CreateOrGetUserAsync(string userId, string nickname)
    {
        var client = _httpClientFactory.CreateClient("Sendbird");

        using var getReq = BuildRequest(HttpMethod.Get, $"/users/{userId}");
        using var getResp = await client.SendAsync(getReq);

        if (getResp.IsSuccessStatusCode)
            return userId;

        if (getResp.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var err = await getResp.Content.ReadAsStringAsync();
            throw new Exception($"GET failed: {(int)getResp.StatusCode} {err}");
        }

        using var createReq = BuildRequest(HttpMethod.Post, "/users", new
        {
            user_id = userId,
            nickname = nickname
        });

        using var createResp = await client.SendAsync(createReq);

        var body = await createResp.Content.ReadAsStringAsync();

        if (!createResp.IsSuccessStatusCode)
            throw new Exception($"CREATE failed: {(int)createResp.StatusCode} {body}");

        return userId;
    }

    /// <summary>
    /// Creates a distinct 1:1 group channel between renter and owner for a specific item.
    /// Returns the channel_url. Re-uses an existing channel if one already exists.
    /// </summary>
    public async Task<SendbirdChannel> CreateGroupChannelAsync(string renterId, string ownerId, string itemName)
    {
        var client = _httpClientFactory.CreateClient();

        await CreateOrGetUserAsync(renterId, renterId);
        await CreateOrGetUserAsync(ownerId, ownerId);

        using var req = BuildRequest(HttpMethod.Post, "/group_channels", new
        {
            user_ids = new[] { renterId, ownerId },
            is_distinct = true,
            name = $"Chat: {itemName}",
            custom_type = "item_chat"
        });

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var channel = await resp.Content.ReadFromJsonAsync<SendbirdChannel>(_json);
        return channel ?? throw new Exception("Failed to create Sendbird channel");
    }

    /// <summary>
    /// Lists group channels the given user belongs to.
    /// </summary>
    public async Task<List<SendbirdChannel>> ListUserChannelsAsync(string userId)
    {
        var client = _httpClientFactory.CreateClient();

        using var req = BuildRequest(HttpMethod.Get,
            $"/users/{userId}/my_group_channels?limit=20&order=latest_last_message");
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
}

public class ChannelListResponse
{
    [JsonPropertyName("channels")]
    public List<SendbirdChannel> Channels { get; set; } = new();
}
