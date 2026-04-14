using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ToolPool.Models;

namespace ToolPool.Services;

public class SupabaseDemoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SupabaseOptions _opt;
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public SupabaseDemoService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _opt = config.GetSection("Supabase").Get<SupabaseOptions>() ?? new SupabaseOptions();
    }

    public async Task<List<ToolCategory>> GetCategories()
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Categories";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<List<ToolCategory>>(_jsonOpts);
        return items ?? new List<ToolCategory>();
    }

    public async Task<List<Tool>> GetToolsAsync()
    {
        var client = _httpClientFactory.CreateClient();
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
            return await fallbackResp.Content.ReadFromJsonAsync<List<Tool>>(_jsonOpts) ?? new List<Tool>();
        }

        return await resp.Content.ReadFromJsonAsync<List<Tool>>(_jsonOpts) ?? new List<Tool>();
    }

    public async Task<Tool?> GetToolByIdAsync(Guid id)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Tools?id=eq.{id}&select=id,name,description,price,category,owner_id,owner_name,neighborhood,image_url,created_at";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _opt.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AnonKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var items = await resp.Content.ReadFromJsonAsync<List<Tool>>(_jsonOpts);
        return items?.FirstOrDefault();
    }

    public async Task<InterestSubmission> InsertInterestAsync(InterestSubmission interest)
    {
        var client = _httpClientFactory.CreateClient();
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


    public async Task<Tool> InsertToolAsync(string name, string description, decimal price,
        string category = "", Guid? ownerId = null, string ownerName = "", string neighborhood = "", string imageUrl = "")
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Tools";

        var payload = new
        {
            name,
            description,
            price,
            category,
            owner_id = ownerId,
            owner_name = ownerName,
            neighborhood,
            image_url = imageUrl
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
        req.Headers.Add("Prefer", "return=representation");
        req.Content = JsonContent.Create(payload);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var inserted = await resp.Content.ReadFromJsonAsync<List<Tool>>(_jsonOpts);
        return inserted?.FirstOrDefault() ?? new Tool { Name = name, Description = description, Price = price };
    }


    public async Task DeleteToolAsync(Guid id)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Tools?id=eq.{id}";

        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task SeedToolsAsync()
    {
        var existing = await GetToolsAsync();
        var hasSeeded = existing.Any(t => !string.IsNullOrEmpty(t.Category));
        if (hasSeeded) return;

        // Delete old tools that lack category data (pre-migration leftovers)
        foreach (var old in existing.Where(t => string.IsNullOrEmpty(t.Category)))
        {
            await DeleteToolAsync(old.Id);
        }

        var seeds = new[]
        {
            new { name = "DeWalt Power Drill", description = "Cordless 20V drill with two batteries and charger", price = 12.00m, category = "Power Tools", owner_name = "Mike T.", neighborhood = "Southend", image_url = "https://images.unsplash.com/photo-1572981779307-38b8cabb2407?w=400&fit=crop" },
            new { name = "Circular Saw", description = "7-1/4 inch blade, great for framing and decking", price = 18.00m, category = "Power Tools", owner_name = "Emma L.", neighborhood = "Westside", image_url = "https://images.unsplash.com/photo-1504148455328-c376907d081c?w=400&fit=crop" },
            new { name = "Hand Tool Set", description = "Complete 50-piece set with wrenches, pliers, and screwdrivers", price = 8.00m, category = "Hand Tools", owner_name = "James K.", neighborhood = "Midtown", image_url = "https://images.unsplash.com/photo-1581783898377-1c85bf937427?w=400&fit=crop" },
            new { name = "Ladder (20ft)", description = "Extension ladder, aluminum, supports up to 250 lbs", price = 15.00m, category = "Equipment", owner_name = "Mike T.", neighborhood = "Downtown", image_url = "https://images.unsplash.com/photo-1585771724684-38269d6639fd?w=400&fit=crop" },
            new { name = "Pressure Washer", description = "2000 PSI electric pressure washer with hose and nozzles", price = 25.00m, category = "Cleaning", owner_name = "Sarah M.", neighborhood = "Eastside", image_url = "https://images.unsplash.com/photo-1622735620941-e8192a023a3f?w=400&fit=crop" },
            new { name = "Hedge Trimmer", description = "24-inch cordless hedge trimmer, battery included", price = 10.00m, category = "Garden", owner_name = "Emma L.", neighborhood = "Uptown", image_url = "https://images.unsplash.com/photo-1416879595882-3373a0480b5b?w=400&fit=crop" },
            new { name = "Tile Cutter", description = "Manual tile cutter for ceramic and porcelain up to 24 inches", price = 14.00m, category = "Hand Tools", owner_name = "Chen W.", neighborhood = "Downtown", image_url = "https://images.unsplash.com/photo-1558618666-fcd25c85f82e?w=400&fit=crop" },
            new { name = "Shop Vacuum", description = "6-gallon wet/dry shop vac with attachments", price = 9.00m, category = "Cleaning", owner_name = "James K.", neighborhood = "Southend", image_url = "https://images.unsplash.com/photo-1558317374-067fb5f30001?w=400&fit=crop" },
        };

        var client = _httpClientFactory.CreateClient();
        var url = $"{_opt.Url}/rest/v1/Tools";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("apikey", _opt.ServiceRoleKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ServiceRoleKey);
        req.Headers.Add("Prefer", "return=representation");
        req.Content = JsonContent.Create(seeds);

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }
}
