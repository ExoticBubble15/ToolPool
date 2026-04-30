using System.Net.Http.Headers;
using System.Text.Json;
using ToolPool.Client.Models;
using System.Text.Json.Serialization;
using ToolPool.Models;
using Supabase.Gotrue;

namespace ToolPool.Services;

public class SupabaseDemoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SupabaseOptions _opt;
    private Dictionary<String, ToolPool.Models.User> _users;
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };
    private HttpClient client;

    public SupabaseDemoService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _opt = config.GetSection("Supabase").Get<SupabaseOptions>() ?? new SupabaseOptions();
        client = _httpClientFactory.CreateClient(); //remove this if everything breaks
    }

    public async Task<List<ToolPool.Models.DemoItem>> GetDemoItemsAsync()
    {
        var url = $"{_opt.Url}/rest/v1/Tools?select=id,name,description,price&order=created_at.desc";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<List<ToolPool.Models.DemoItem>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return items ?? new List<ToolPool.Models.DemoItem>();
    }

    public async Task InsertSubmissionAsync(
        string name,
        string description,
        decimal price,
        string ownerId,
        string category,
        string ownerName,
        string neighborhood,
        string imageUrl)
    {
        var url = $"{_opt.Url}/rest/v1/Tools";

        var payload = new
        {
            name = name.Trim(),
            description = description.Trim(),
            price = price,
            created_at = DateTime.UtcNow,
            image_url = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            category = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            neighborhood = string.IsNullOrWhiteSpace(neighborhood) ? null : neighborhood.Trim(),
            owner_id = ownerId,
            owner_name = ownerName
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);

        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        req.Headers.Add("Prefer", "return=representation");

        req.Content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var resp = await client.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            throw new Exception($"Insert failed: {resp.StatusCode} - {body}");
        }
    }

    public async Task<ToolPool.Models.DemoItem> InsertDemoItemAsync(string name, string description, decimal price)
    {
        var url = $"{_opt.Url}/rest/v1/Tools";

        var payload = new
        {
            name,
            description,
            price
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
        req.Headers.Add("Prefer", "return=representation");
        req.Content = JsonContent.Create(payload);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var inserted = await resp.Content.ReadFromJsonAsync<List<ToolPool.Models.DemoItem>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return inserted?.FirstOrDefault() ?? new ToolPool.Models.DemoItem
        {
            Name = name,
            Description = description,
            Price = price
        };
    }

    //public async Task<List<DemoItemSubmission>> GetLatestSubmissionsAsync(int limit = 5)
    //{
    //    var url = $"{_opt.Url}/rest/v1/Tools_submissions?select=id,name,description,price,submitted_at&order=submitted_at.desc&limit={limit}";

    //    using var req = new HttpRequestMessage(HttpMethod.Get, url);
    //    req.Headers.Add("apikey", _opt.AnonKey);
    //    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

    //    using var resp = await client.SendAsync(req);
    //    resp.EnsureSuccessStatusCode();

    //    var rows = await resp.Content.ReadFromJsonAsync<List<DemoItemSubmission>>(new JsonSerializerOptions
    //    {
    //        PropertyNameCaseInsensitive = true
    //    });

    //    return rows ?? new List<DemoItemSubmission>();
    //}
    public async Task DeleteDemoItemAsync(Guid id)
    {
        var url = $"{_opt.Url}/rest/v1/Tools?id=eq.{id}";

        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task InsertUserAsync(object payload)
    {
        var url = $"{_opt.Url}/rest/v1/Users";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);

        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        req.Headers.Add("Prefer", "return=representation");
        req.Content = JsonContent.Create(payload);

        using var resp = await client.SendAsync(req);

        var body = await resp.Content.ReadAsStringAsync();
        Console.WriteLine("SUPABASE INSERT RESPONSE:");
        Console.WriteLine(body);

        resp.EnsureSuccessStatusCode();
    }

    public async Task UpdateUserSessionAsync(string email, Session newSession)
    {
        var url = $"{_opt.Url}/rest/v1/Users?email=eq.{Uri.EscapeDataString(email)}";

        using var req = new HttpRequestMessage(HttpMethod.Patch, url);

        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        req.Content = JsonContent.Create(new { access_token = newSession.AccessToken, refresh_token = newSession.RefreshToken });

        using var resp = await client.SendAsync(req);

        var body = await resp.Content.ReadAsStringAsync();
        Console.WriteLine("SUPABASE INSERT RESPONSE:");
        Console.WriteLine(body);

        resp.EnsureSuccessStatusCode();
    }

    public async Task<ToolPool.Models.User?> GetUserAsync(string email)
    {
        var url = $"{_opt.Url}/rest/v1/Users?email=eq.{email}&select=*&limit=1";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        var users = await resp.Content.ReadFromJsonAsync<List<ToolPool.Models.User>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return users?.FirstOrDefault();
    }

    public async Task<ProfileUserDto?> GetProfileUserAsync(Guid userId)
    {
        var url = $"{_opt.Url}/rest/v1/Users?id=eq.{userId}&select=id,email,username,created_at,updated_at,stripe_account_id,stripe_customer_id,sendbird_user_id&limit=1";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var users = await resp.Content.ReadFromJsonAsync<List<ProfileUserDto>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return users?.FirstOrDefault();
    }

    /// <summary>
    /// Asynchronously retrieves the Stripe destination account identifier associated with the owner of the specified
    /// tool.
    /// </summary>
    /// <remarks>This method queries the backend service to determine the owner of the specified tool and then
    /// retrieves the Stripe account identifier for that owner. </remarks>
    /// <param name="toolId">The unique identifier of the tool for which to retrieve the owner's Stripe destination account. Must be a valid,
    /// existing tool ID.</param>
    /// <returns>A string containing the Stripe account identifier if found; otherwise, null if the tool or its owner does not
    /// exist or does not have a Stripe account associated.</returns>
    public async Task<string?> GetStripeDestinationForToolAsync(Guid toolId)
    {
        // api request
        var url = $"{_opt.Url}/rest/v1/Tools?id=eq.{toolId}&select=owner_id&limit=1";

        // make api request
        using var toolReq = new HttpRequestMessage(HttpMethod.Get, url);
        toolReq.Headers.Add("apikey", _opt.ServiceRoleKey);
        toolReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        // get response
        using var toolResp = await client.SendAsync(toolReq);
        toolResp.EnsureSuccessStatusCode();

        // get the tools from response 
        var tools = await toolResp.Content.ReadFromJsonAsync<List<ToolOwnerLookupDto>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // get the owner from the tool
        var ownerId = tools?.FirstOrDefault()?.OwnerId;
        if (ownerId is null)
        {
            return null;
        }

        // api request to get the strip account for the owner
        var ownerUrl = $"{_opt.Url}/rest/v1/Users?id=eq.{ownerId}&select=stripe_account_id&limit=1";

        // make request
        using var ownerReq = new HttpRequestMessage(HttpMethod.Get, ownerUrl);
        ownerReq.Headers.Add("apikey", _opt.ServiceRoleKey);
        ownerReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        // get response
        using var ownerResp = await client.SendAsync(ownerReq);
        ownerResp.EnsureSuccessStatusCode();

        // get the owner 
        var owners = await ownerResp.Content.ReadFromJsonAsync<List<StripeAccountLookupDto>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // return stripe id
        return owners?.FirstOrDefault()?.StripeAccountId;
    }

    public async Task<List<ProfileListingDto>> GetListingsByOwnerAsync(Guid ownerId)
    {
        var url = $"{_opt.Url}/rest/v1/Tools?owner_id=eq.{ownerId}&select=id,name,description,price,category,neighborhood,image_url,owner_id,owner_name,created_at&order=created_at.desc";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var listings = await resp.Content.ReadFromJsonAsync<List<ProfileListingDto>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return listings ?? new List<ProfileListingDto>();
    }

    public async Task<List<ProfileActivityDto>> GetActivitiesByUserAsync(Guid userId)
    {
        var filter = Uri.EscapeDataString($"(owner_id.eq.{userId},renter_id.eq.{userId})");
        var url = $"{_opt.Url}/rest/v1/Interest_Submissions?or={filter}&select=id,tool_id,tool_name,renter_id,owner_id,message,start_date,end_date,channel_url,status,created_at&order=created_at.desc";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var activities = await resp.Content.ReadFromJsonAsync<List<ProfileActivityDto>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var results = activities ?? new List<ProfileActivityDto>();
        if (!results.Any())
        {
            return results;
        }

        var missingToolNameIds = results
            .Where(x => string.IsNullOrWhiteSpace(x.ToolName) && x.ToolId != Guid.Empty)
            .Select(x => x.ToolId)
            .Distinct()
            .ToList();

        var toolNameLookup = missingToolNameIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await GetToolNamesByIdsAsync(missingToolNameIds);

        var userCache = new Dictionary<Guid, Models.AppUser?>();

        foreach (var item in results)
        {
            if (string.IsNullOrWhiteSpace(item.ToolName)
                && item.ToolId != Guid.Empty
                && toolNameLookup.TryGetValue(item.ToolId, out var resolvedToolName))
            {
                item.ToolName = resolvedToolName;
            }

            item.Status = string.IsNullOrWhiteSpace(item.Status)
                ? "pending"
                : item.Status.Trim().ToLowerInvariant();

            if (item.OwnerId == userId)
            {
                item.Role = "owner";
                if (Guid.TryParse(item.RenterId, out var renterGuid))
                {
                    if (!userCache.TryGetValue(renterGuid, out var renter))
                    {
                        renter = await GetUserByIdAsync(renterGuid);
                        userCache[renterGuid] = renter;
                    }

                    item.CounterpartName = renter?.Username ?? renter?.Email ?? "Renter";
                }
                else
                {
                    item.CounterpartName = "Renter";
                }
            }
            else
            {
                item.Role = "renter";
                if (item.OwnerId.HasValue)
                {
                    var ownerGuid = item.OwnerId.Value;
                    if (!userCache.TryGetValue(ownerGuid, out var owner))
                    {
                        owner = await GetUserByIdAsync(ownerGuid);
                        userCache[ownerGuid] = owner;
                    }

                    item.CounterpartName = owner?.Username ?? owner?.Email ?? "Owner";
                }
                else
                {
                    item.CounterpartName = "Owner";
                }
            }
        }

        return results;
    }

    private async Task<Dictionary<Guid, string>> GetToolNamesByIdsAsync(IEnumerable<Guid> toolIds)
    {
        var ids = toolIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var inClause = string.Join(",", ids.Select(x => x.ToString()));
        var url = $"{_opt.Url}/rest/v1/Tools?id=in.({inClause})&select=id,name";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var resp = await client.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            return new Dictionary<Guid, string>();
        }

        var rows = await resp.Content.ReadFromJsonAsync<List<ToolNameLookupDto>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return rows?
            .Where(x => x.Id != Guid.Empty && !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.First().Name) ?? new Dictionary<Guid, string>();
    }

    public async Task DeleteRatingsByUserAsync(Guid userId)
    {
        var filter = Uri.EscapeDataString($"(rater_id.eq.{userId},rated_user_id.eq.{userId})");
        var url = $"{_opt.Url}/rest/v1/Ratings?or={filter}";
        await SendDeleteAsync(url);
    }

    public async Task DeleteInterestSubmissionsByUserAsync(Guid userId)
    {
        var filter = Uri.EscapeDataString($"(owner_id.eq.{userId},renter_id.eq.{userId})");
        var url = $"{_opt.Url}/rest/v1/Interest_Submissions?or={filter}";
        await SendDeleteAsync(url);
    }

    public async Task DeleteToolsByOwnerAsync(Guid userId)
    {
        var url = $"{_opt.Url}/rest/v1/Tools?owner_id=eq.{userId}";
        await SendDeleteAsync(url);
    }

    public async Task DeletePublicUserAsync(Guid userId)
    {
        var url = $"{_opt.Url}/rest/v1/Users?id=eq.{userId}";
        await SendDeleteAsync(url);
    }

    public async Task DeleteAuthUserAsync(Guid userId)
    {
        var authUrl = $"{_opt.Url.TrimEnd('/')}/auth/v1";
        var options = new Supabase.Gotrue.ClientOptions
        {
            Url = authUrl,
            Headers = new Dictionary<string, string>
            {
                ["apikey"] = _opt.ServiceRoleKey
            }
        };

        var adminClient = new Supabase.Gotrue.AdminClient(_opt.ServiceRoleKey, options);
        await adminClient.DeleteUser(userId.ToString());
    }

    private async Task SendDeleteAsync(string url)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    //query neighborhood, latitude, longitude from db
    public async Task<List<NeighborhoodTriple>> GetNeighborhoodTriples()
    {
        var baseUrl = (_opt.Url ?? string.Empty).TrimEnd('/');
        var url = $"{baseUrl}/rest/v1/Neighborhoods?select=neighborhood,latitude,longitude";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var triples = await resp.Content.ReadFromJsonAsync<List<NeighborhoodTriple>>(_jsonOpts);
        return triples ?? new List<NeighborhoodTriple>();
    }

    //query all categories, regardless if any tools associated with them
    public async Task<List<ToolCategory>> GetCategories()
    {
        var url = $"{_opt.Url}/rest/v1/Categories";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<List<ToolCategory>>(_jsonOpts);
        return items ?? new List<ToolCategory>();
    }

    public async Task<Models.AppUser?> GetUserByIdAsync(Guid id)
    {
        var url = $"{_opt.Url}/rest/v1/Users?id=eq.{id.ToString().ToLower()}&select=id,email,username,sendbird_user_id,stripe_account_id,created_at";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<List<Models.AppUser>>(_jsonOpts);
        return items?.FirstOrDefault();
    }

    public async Task<Models.AppUser?> GetUserByEmailAsync(string email)
    {
        var url = $"{_opt.Url}/rest/v1/Users?email=eq.{Uri.EscapeDataString(email)}&select=id,email,username,sendbird_user_id,created_at";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<List<Models.AppUser>>(_jsonOpts);
        return items?.FirstOrDefault();
    }

    public async Task<Models.AppUser> CreateUserAsync(Guid id, string email, string username)
    {
        var url = $"{_opt.Url}/rest/v1/Users";

        var payload = new { id, email, username, sendbird_user_id = id.ToString() };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
        req.Headers.Add("Prefer", "return=representation");
        req.Content = JsonContent.Create(payload);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var inserted = await resp.Content.ReadFromJsonAsync<List<Models.AppUser>>(_jsonOpts);
        return inserted?.FirstOrDefault() ?? new Models.AppUser { Id = id, Email = email, Username = username, SendbirdUserId = id.ToString() };
    }

    public async Task UpdateUserSendbirdIdAsync(Guid userId, string sendbirdUserId)
    {
        var url = $"{_opt.Url}/rest/v1/Users?id=eq.{userId}";

        using var req = new HttpRequestMessage(HttpMethod.Patch, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
        req.Content = JsonContent.Create(new { sendbird_user_id = sendbirdUserId });

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<InterestSubmission>> GetInterestsByRenterAsync(string renterId)
    {
        var url = $"{_opt.Url}/rest/v1/Interest_Submissions?renter_id=eq.{Uri.EscapeDataString(renterId)}&order=created_at.desc";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        return await resp.Content.ReadFromJsonAsync<List<InterestSubmission>>(_jsonOpts) ?? new();
    }

    public async Task<List<InterestSubmission>> GetInterestsByOwnerAsync(Guid ownerId)
    {
        var url = $"{_opt.Url}/rest/v1/Interest_Submissions?owner_id=eq.{ownerId}&order=created_at.desc";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        return await resp.Content.ReadFromJsonAsync<List<InterestSubmission>>(_jsonOpts) ?? new();
    }

    public async Task<InterestSubmission?> GetInterestByRenterAndToolAsync(string renterId, Guid toolId)
    {
        var url = $"{_opt.Url}/rest/v1/Interest_Submissions?renter_id=eq.{Uri.EscapeDataString(renterId)}&tool_id=eq.{toolId}&order=created_at.desc&limit=1";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var results = await resp.Content.ReadFromJsonAsync<List<InterestSubmission>>(_jsonOpts);
        return results?.FirstOrDefault();
    }

    public async Task UpdateInterestChannelUrlAsync(Guid interestId, string channelUrl)
    {
        var url = $"{_opt.Url}/rest/v1/Interest_Submissions?id=eq.{interestId}";

        using var req = new HttpRequestMessage(HttpMethod.Patch, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
        req.Content = JsonContent.Create(new { channel_url = channelUrl });

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<InterestSubmission?> GetInterestByIdAsync(Guid interestId)
    {
        var url = $"{_opt.Url}/rest/v1/Interest_Submissions?id=eq.{interestId}&limit=1";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var results = await resp.Content.ReadFromJsonAsync<List<InterestSubmission>>(_jsonOpts);
        return results?.FirstOrDefault();
    }

    public async Task UpdateInterestStatusAsync(Guid interestId, string status)
    {
        var url = $"{_opt.Url}/rest/v1/Interest_Submissions?id=eq.{interestId}";

        using var req = new HttpRequestMessage(HttpMethod.Patch, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
        req.Content = JsonContent.Create(new { status });

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    // ── Tool queries ──
    //query all tools
    public async Task<List<Models.Tool>> GetToolsAsync()
    {
        var url = $"{_opt.Url}/rest/v1/Tools?order=created_at.desc";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);

        if (!resp.IsSuccessStatusCode)
        {
            // Fallback: new columns may not exist yet -- query basic columns only
            var fallbackUrl = $"{_opt.Url}/rest/v1/Tools?select=id,name,description,price,owner_id,created_at&order=created_at.desc";
            using var fallbackReq = new HttpRequestMessage(HttpMethod.Get, fallbackUrl);
            fallbackReq.Headers.Add("apikey", _opt.AnonKey);
            fallbackReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

            using var fallbackResp = await client.SendAsync(fallbackReq);
            fallbackResp.EnsureSuccessStatusCode();
            return await fallbackResp.Content.ReadFromJsonAsync<List<Models.Tool>>(_jsonOpts) ?? new List<Models.Tool>();
        }

        return await resp.Content.ReadFromJsonAsync<List<Models.Tool>>(_jsonOpts) ?? new List<Models.Tool>();
    }

    //query specific tool by id
    public async Task<Models.Tool?> GetToolByIdAsync(Guid id)
    {
        var url = $"{_opt.Url}/rest/v1/Tools?id=eq.{id}&select=id,name,description,price,category,owner_id,owner_name,neighborhood,image_url,created_at";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<List<Models.Tool>>(_jsonOpts);
        return items?.FirstOrDefault();
    }

    public async Task<Models.ToolAddressLookup?> GetToolAddressByIdAsync(Guid id)
    {
        var url = $"{_opt.Url}/rest/v1/Tools?id=eq.{id}&select=id,name,owner_id,addressLat,addressLng,neighborhood&limit=1";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<List<Models.ToolAddressLookup>>(_jsonOpts);
        return items?.FirstOrDefault();
    }

    //query rating for a user
    public async Task<Models.OwnerRating?> GetOwnerRatingAsync(Guid ownerId)
    {
        var url = $"{_opt.Url}/rest/v1/Users?id=eq.{ownerId}&select=avg_rating,total_ratings";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;

        var rows = await resp.Content.ReadFromJsonAsync<List<Models.OwnerRating>>(_jsonOpts);
        return rows?.FirstOrDefault();
    }

    // ── Ratings ──

    public async Task<Models.Rating?> GetRatingAsync(Guid interestId, Guid raterId, Guid ratedUserId)
    {
        var url = $"{_opt.Url}/rest/v1/Ratings?interest_id=eq.{interestId}&rater_id=eq.{raterId}&rated_user_id=eq.{ratedUserId}&limit=1";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;

        var rows = await resp.Content.ReadFromJsonAsync<List<Models.Rating>>(_jsonOpts);
        return rows?.FirstOrDefault();
    }

    public async Task UpsertRatingAsync(Guid interestId, Guid raterId, Guid ratedUserId, int score)
    {
        var existing = await GetRatingAsync(interestId, raterId, ratedUserId);

        if (existing is not null)
        {
            var patchUrl = $"{_opt.Url}/rest/v1/Ratings?id=eq.{existing.Id}";
            using var patchReq = new HttpRequestMessage(HttpMethod.Patch, patchUrl);
            patchReq.Headers.Add("apikey", _opt.ServiceRoleKey);
            patchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
            patchReq.Content = JsonContent.Create(new { score });

            using var patchResp = await client.SendAsync(patchReq);
            patchResp.EnsureSuccessStatusCode();
            return;
        }

        var insertUrl = $"{_opt.Url}/rest/v1/Ratings";
        using var insertReq = new HttpRequestMessage(HttpMethod.Post, insertUrl);
        insertReq.Headers.Add("apikey", _opt.ServiceRoleKey);
        insertReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
        insertReq.Headers.Add("Prefer", "return=representation");
        insertReq.Content = JsonContent.Create(new
        {
            interest_id = interestId,
            rater_id = raterId,
            rated_user_id = ratedUserId,
            score
        });

        using var insertResp = await client.SendAsync(insertReq);
        insertResp.EnsureSuccessStatusCode();
    }

    public async Task RecomputeUserAggregateAsync(Guid ratedUserId)
    {
        var listUrl = $"{_opt.Url}/rest/v1/Ratings?rated_user_id=eq.{ratedUserId}&select=score";
        using var listReq = new HttpRequestMessage(HttpMethod.Get, listUrl);
        listReq.Headers.Add("apikey", _opt.ServiceRoleKey);
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var listResp = await client.SendAsync(listReq);
        listResp.EnsureSuccessStatusCode();

        var rows = await listResp.Content.ReadFromJsonAsync<List<Models.Rating>>(_jsonOpts) ?? new();
        var total = rows.Count;
        double? avg = total > 0 ? Math.Round(rows.Average(r => (double)r.Score), 2) : null;

        var patchUrl = $"{_opt.Url}/rest/v1/Users?id=eq.{ratedUserId}";
        using var patchReq = new HttpRequestMessage(HttpMethod.Patch, patchUrl);
        patchReq.Headers.Add("apikey", _opt.ServiceRoleKey);
        patchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
        patchReq.Content = JsonContent.Create(new { avg_rating = avg, total_ratings = total });

        using var patchResp = await client.SendAsync(patchReq);
        patchResp.EnsureSuccessStatusCode();
    }

    public async Task<InterestSubmission> InsertInterestAsync(InterestSubmission interest)
    {
        var url = $"{_opt.Url}/rest/v1/Interest_Submissions";

        var payload = new
        {
            tool_id = interest.ToolId,
            tool_name = interest.ToolName,
            renter_id = interest.RenterId,
            owner_id = interest.OwnerId,
            message = interest.Message,
            start_date = interest.StartDate,
            end_date = interest.EndDate,
            channel_url = interest.ChannelUrl,
            status = interest.Status
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
        req.Headers.Add("Prefer", "return=representation");
        req.Content = JsonContent.Create(payload);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var inserted = await resp.Content.ReadFromJsonAsync<List<InterestSubmission>>(_jsonOpts);
        return inserted?.FirstOrDefault() ?? interest;
    }

    //insert tool to db
    public async Task<Models.Tool> InsertToolAsync(Models.Tool payload)
    {
        var url = $"{_opt.Url}/rest/v1/Tools";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
        req.Headers.Add("Prefer", "return=representation");
        req.Content = JsonContent.Create(payload);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var inserted = await resp.Content.ReadFromJsonAsync<List<Models.Tool>>(_jsonOpts);
        return inserted?.FirstOrDefault() ?? new Models.Tool();
    }

    //delete specific tool by id
    public async Task DeleteToolAsync(Guid id)
    {
        var url = $"{_opt.Url}/rest/v1/Tools?id=eq.{id}";

        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Determines whether the specified time range for a tool overlaps with any existing bookings.
    /// </summary>
    /// <remarks>The method checks for any overlap between the specified time range and existing bookings for
    /// the given tool. Both start and end times are inclusive.</remarks>
    /// <param name="toolId">The unique identifier of the tool to check for booking conflicts.</param>
    /// <param name="start">The start date and time of the proposed booking period.</param>
    /// <param name="end">The end date and time of the proposed booking period.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if there is a
    /// booking conflict for the specified tool and time range; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> HasBookingConflictAsync(Guid toolId, DateTime start, DateTime end)
    {
        // build request URL to retrieve all booking date ranges associated with the specified tool
        var url = $"{_opt.Url}/rest/v1/Interest_Submissions?" +
                  $"tool_id=eq.{toolId}" +
                  $"&select=start_date,end_date";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var bookings = await resp.Content
            .ReadFromJsonAsync<List<InterestSubmission>>(_jsonOpts)
            ?? new();

        foreach (var b in bookings)
        {
            if (!DateTime.TryParse(b.StartDate, out var bStart)) continue;
            if (!DateTime.TryParse(b.EndDate, out var bEnd)) continue;

            // overlap check
            bool overlap = start <= bEnd && end >= bStart;

            if (overlap)
                return true;
        }

        return false;
    }
}
public class ProfileUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }

    [JsonPropertyName("stripe_account_id")]
    public string? StripeAccountId { get; set; }

    [JsonPropertyName("stripe_customer_id")]
    public string? StripeCustomerId { get; set; }

    [JsonPropertyName("sendbird_user_id")]
    public string? SendbirdUserId { get; set; }
}


public class ProfileListingDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Category { get; set; }
    public string? Neighborhood { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("owner_id")]
    public Guid? OwnerId { get; set; }

    [JsonPropertyName("owner_name")]
    public string? OwnerName { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
}

public class ProfileActivityDto
{
    public Guid Id { get; set; }

    [JsonPropertyName("tool_id")]
    public Guid ToolId { get; set; }

    [JsonPropertyName("tool_name")]
    public string ToolName { get; set; } = string.Empty;

    [JsonPropertyName("renter_id")]
    public string? RenterId { get; set; }

    [JsonPropertyName("owner_id")]
    public Guid? OwnerId { get; set; }

    public string? Message { get; set; }

    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }

    [JsonPropertyName("channel_url")]
    public string? ChannelUrl { get; set; }

    public string? Status { get; set; }

    [JsonPropertyName("counterpart_name")]
    public string? CounterpartName { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
}

public class ToolOwnerLookupDto
{
    [JsonPropertyName("owner_id")]
    public Guid? OwnerId { get; set; }
}

public class StripeAccountLookupDto
{
    [JsonPropertyName("stripe_account_id")]
    public string? StripeAccountId { get; set; }
}

public class ToolNameLookupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
