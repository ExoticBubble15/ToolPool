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
        var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Tools?select=id,name,description,price&order=created_at.desc";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<List<ToolPool.Models.DemoItem >> (new JsonSerializerOptions
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
        var client = _httpClientFactory.CreateClient();
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
        var client = _httpClientFactory.CreateClient();
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
    //    var client = _httpClientFactory.CreateClient();
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
        var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Tools?id=eq.{id}";

        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task InsertUserAsync(object payload)
    {
        var client = _httpClientFactory.CreateClient();
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
        var client = _httpClientFactory.CreateClient();
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
        var client = _httpClientFactory.CreateClient();
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
        var client = _httpClientFactory.CreateClient();
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

    public async Task<string?> GetStripeDestinationForToolAsync(Guid toolId)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Tools?id=eq.{toolId}&select=owner_id&limit=1";

        using var toolReq = new HttpRequestMessage(HttpMethod.Get, url);
        toolReq.Headers.Add("apikey", _opt.ServiceRoleKey);
        toolReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var toolResp = await client.SendAsync(toolReq);
        toolResp.EnsureSuccessStatusCode();

        var tools = await toolResp.Content.ReadFromJsonAsync<List<ToolOwnerLookupDto>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var ownerId = tools?.FirstOrDefault()?.OwnerId;
        if (ownerId is null)
        {
            return null;
        }

        var ownerUrl = $"{_opt.Url}/rest/v1/Users?id=eq.{ownerId}&select=stripe_account_id&limit=1";

        using var ownerReq = new HttpRequestMessage(HttpMethod.Get, ownerUrl);
        ownerReq.Headers.Add("apikey", _opt.ServiceRoleKey);
        ownerReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var ownerResp = await client.SendAsync(ownerReq);
        ownerResp.EnsureSuccessStatusCode();

        var owners = await ownerResp.Content.ReadFromJsonAsync<List<StripeAccountLookupDto>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return owners?.FirstOrDefault()?.StripeAccountId;
    }

    public async Task<List<ProfileListingDto>> GetListingsByOwnerAsync(Guid ownerId)
    {
        var client = _httpClientFactory.CreateClient();
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
        var client = _httpClientFactory.CreateClient();
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

        var client = _httpClientFactory.CreateClient();
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
        var client = _httpClientFactory.CreateClient();

        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<MarkerDetails>> GetMarkerDetails()
    {
        var baseUrl = (_opt.Url ?? string.Empty).TrimEnd('/');
        var url = $"{baseUrl}/rest/v1/Tools?select=id,name,description,owner_name,price,addressLat,addressLng";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var triples = await resp.Content.ReadFromJsonAsync<List<MarkerDetails>>(_jsonOpts);
        return triples ?? new List<MarkerDetails>();
    }

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

    public async Task<List<ToolCategory>> GetCategories()
    {
        //var client = _httpClientFactory.CreateClient();
        //var url = $"{_opt.Url}/rest/v1/Tools?select=category";
        var url = $"{_opt.Url}/rest/v1/Categories";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<List<ToolCategory>>(_jsonOpts);
        return items ?? new List<ToolCategory>();
    }

    public async Task<List<ToolNeighborhood>> GetNeighborhoods()
    {
        //var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Tools?select=neighborhood";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<List<ToolNeighborhood>>(_jsonOpts);
        return items ?? new List<ToolNeighborhood>();
    }

    //public async Task<List<NeighborhoodTuple>> GetCityNeighborhoods()
    //{
    //    var client = _httpClientFactory.CreateClient();
    //    var url = $"{_opt.Url}/rest/v1/Neighborhoods?select=neighborhood,city";

    //    using var req = new HttpRequestMessage(HttpMethod.Get, url);
    //    req.Headers.Add("apikey", _opt.AnonKey);
    //    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

    //    using var resp = await client.SendAsync(req);
    //    resp.EnsureSuccessStatusCode();

    //    var items = await resp.Content.ReadFromJsonAsync<List<NeighborhoodTuple>>(_jsonOpts);
    //    return items ?? new List<NeighborhoodTuple>();
    //}

    // ── User queries ──

    public async Task<Models.AppUser?> GetUserByIdAsync(Guid id)
    {
        //var client = _httpClientFactory.CreateClient();
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
        //var client = _httpClientFactory.CreateClient();
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
        //var client = _httpClientFactory.CreateClient();
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
        //var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Users?id=eq.{userId}";

        using var req = new HttpRequestMessage(HttpMethod.Patch, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
        req.Content = JsonContent.Create(new { sendbird_user_id = sendbirdUserId });

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    // ── Interest queries ──

    public async Task<List<InterestSubmission>> GetInterestsByRenterAsync(string renterId)
    {
        //var client = _httpClientFactory.CreateClient();
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
        //var client = _httpClientFactory.CreateClient();
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
        //var client = _httpClientFactory.CreateClient();
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
        //var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Interest_Submissions?id=eq.{interestId}&limit=1";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var results = await resp.Content.ReadFromJsonAsync<List<InterestSubmission>>(_jsonOpts);
        return results?.FirstOrDefault();
    }

    // ── Tool queries ──

    public async Task<List<Models.Tool>> GetToolsAsync()
    {
        //var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Tools?select=id,name,description,price,category,owner_id,owner_name,neighborhood,image_url,created_at&order=created_at.desc";

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

    public async Task<Models.Tool?> GetToolByIdAsync(Guid id)
    {
        //var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Tools?id=eq.{id}&select=id,name,description,price,category,owner_id,owner_name,neighborhood,image_url,created_at";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<List<Models.Tool>>(_jsonOpts);
        return items?.FirstOrDefault();
    }

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

    public async Task<InterestSubmission> InsertInterestAsync(InterestSubmission interest)
    {
        //var client = _httpClientFactory.CreateClient();
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


    public async Task<Models.Tool> InsertToolAsync(Models.Tool payload) 
    {
        //var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Tools";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
        req.Headers.Add("Prefer", "return=representation");
        req.Content = JsonContent.Create(payload);

        using var resp = await client.SendAsync(req);
        //if (!resp.IsSuccessStatusCode)
        //{
        //    var errorContent = await resp.Content.ReadAsStringAsync();
        //    Console.WriteLine($"Status: {(int)resp.StatusCode} {resp.ReasonPhrase}");
        //    Console.WriteLine($"Error: {errorContent}");
        //}
        resp.EnsureSuccessStatusCode();

        var inserted = await resp.Content.ReadFromJsonAsync<List<Models.Tool>>(_jsonOpts);
        return inserted?.FirstOrDefault() ?? new Models.Tool();
    }


    public async Task DeleteToolAsync(Guid id)
    {
        //var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Tools?id=eq.{id}";

        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<bool> HasBookingConflictAsync(Guid toolId, DateTime start, DateTime end)
    {
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

    //public async Task SeedToolsAsync()
    //{
    //    var existing = await GetToolsAsync();
    //    var hasSeeded = existing.Any(t => !string.IsNullOrEmpty(t.Category));
    //    if (hasSeeded) return;

    //    // Delete old tools that lack category data (pre-migration leftovers)
    //    foreach (var old in existing.Where(t => string.IsNullOrEmpty(t.Category)))
    //    {
    //        await DeleteToolAsync(old.Id);
    //    }

    //    var seeds = new[]
    //    {
    //        new { name = "DeWalt Power Drill", description = "Cordless 20V drill with two batteries and charger", price = 12.00m, category = "Power Tools", owner_name = "Mike T.", neighborhood = "Southend", image_url = "https://images.unsplash.com/photo-1572981779307-38b8cabb2407?w=400&fit=crop" },
    //        new { name = "Circular Saw", description = "7-1/4 inch blade, great for framing and decking", price = 18.00m, category = "Power Tools", owner_name = "Emma L.", neighborhood = "Westside", image_url = "https://images.unsplash.com/photo-1504148455328-c376907d081c?w=400&fit=crop" },
    //        new { name = "Hand Tool Set", description = "Complete 50-piece set with wrenches, pliers, and screwdrivers", price = 8.00m, category = "Hand Tools", owner_name = "James K.", neighborhood = "Midtown", image_url = "https://images.unsplash.com/photo-1581783898377-1c85bf937427?w=400&fit=crop" },
    //        new { name = "Ladder (20ft)", description = "Extension ladder, aluminum, supports up to 250 lbs", price = 15.00m, category = "Equipment", owner_name = "Mike T.", neighborhood = "Downtown", image_url = "https://images.unsplash.com/photo-1585771724684-38269d6639fd?w=400&fit=crop" },
    //        new { name = "Pressure Washer", description = "2000 PSI electric pressure washer with hose and nozzles", price = 25.00m, category = "Cleaning", owner_name = "Sarah M.", neighborhood = "Eastside", image_url = "https://images.unsplash.com/photo-1622735620941-e8192a023a3f?w=400&fit=crop" },
    //        new { name = "Hedge Trimmer", description = "24-inch cordless hedge trimmer, battery included", price = 10.00m, category = "Garden", owner_name = "Emma L.", neighborhood = "Uptown", image_url = "https://images.unsplash.com/photo-1416879595882-3373a0480b5b?w=400&fit=crop" },
    //        new { name = "Tile Cutter", description = "Manual tile cutter for ceramic and porcelain up to 24 inches", price = 14.00m, category = "Hand Tools", owner_name = "Chen W.", neighborhood = "Downtown", image_url = "https://images.unsplash.com/photo-1558618666-fcd25c85f82e?w=400&fit=crop" },
    //        new { name = "Shop Vacuum", description = "6-gallon wet/dry shop vac with attachments", price = 9.00m, category = "Cleaning", owner_name = "James K.", neighborhood = "Southend", image_url = "https://images.unsplash.com/photo-1558317374-067fb5f30001?w=400&fit=crop" },
    //    };

    //var client = _httpClientFactory.CreateClient();
    //    var url = $"{_opt.Url}/rest/v1/Tools";

    //    using var req = new HttpRequestMessage(HttpMethod.Post, url);
    //    req.Headers.Add("apikey", _opt.ServiceRoleKey);
    //    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
    //    req.Headers.Add("Prefer", "return=representation");
    //    req.Content = JsonContent.Create(seeds);

    //    using var resp = await client.SendAsync(req);
    //    resp.EnsureSuccessStatusCode();
    //}
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
