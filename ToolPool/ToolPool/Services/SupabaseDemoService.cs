using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
        var url = $"{_opt.Url}/rest/v1/demo_items?select=id,name,description,price&order=created_at.desc";

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

    public async Task InsertSubmissionAsync(string name, string description, decimal price)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/demo_item_submissions";

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
    }

    public async Task<DemoItem> InsertDemoItemAsync(string name, string description, decimal price)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/demo_items";

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

    public async Task<List<DemoItemSubmission>> GetLatestSubmissionsAsync(int limit = 5)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/demo_item_submissions?select=id,name,description,price,submitted_at&order=submitted_at.desc&limit={limit}";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var rows = await resp.Content.ReadFromJsonAsync<List<DemoItemSubmission>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return rows ?? new List<DemoItemSubmission>();
    }
    public async Task DeleteDemoItemAsync(Guid id)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/demo_items?id=eq.{id}";

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

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
        req.Headers.Add("Prefer", "return=representation");

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        var user = await resp.Content.ReadFromJsonAsync<User>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return user;
    }
}
