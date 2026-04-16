using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ToolPool.Models;

namespace ToolPool.Services;

public class SupabaseDemoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SupabaseOptions _opt;

    public SupabaseDemoService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _opt = config.GetSection("Supabase").Get<SupabaseOptions>() ?? new SupabaseOptions();
    }

    public async Task<List<DemoItem>> GetDemoItemsAsync()
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Tools?select=id,name,description,price&order=created_at.desc";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<List<DemoItem>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return items ?? new List<DemoItem>();
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

    public async Task<DemoItem> InsertDemoItemAsync(string name, string description, decimal price)
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

        var inserted = await resp.Content.ReadFromJsonAsync<List<DemoItem>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return inserted?.FirstOrDefault() ?? new DemoItem
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

    public async Task<User?> GetUserAsync(string email) 
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Users?email=eq.{email}&select=*&limit=1";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        var users = await resp.Content.ReadFromJsonAsync<List<User>>(new JsonSerializerOptions
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

        return activities ?? new List<ProfileActivityDto>();
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

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
}
