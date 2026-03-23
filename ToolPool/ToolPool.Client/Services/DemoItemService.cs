using System.Net.Http.Json;
using ToolPool.Client.Models;

namespace ToolPool.Client.Services;

public class DemoItemService
{
    private readonly HttpClient _http;

    public DemoItemService(HttpClient http)
    {
        _http = http;
    }

    public List<DemoItem> GetItems() => new()
{
    new() { Name = "Bike", Description = "It works", Price = 25.00m },
    new() { Name = "DSLR Camera", Description = "Canon EOS R5 with 3 lenses", Price = 80.00m},
    new() { Name = "Camping Tent", Description = " waterproof tent", Price = 20.00m},
    new() { Name = "Kayak", Description = "Single seat, paddle included", Price = 45.00m},
    new() { Name = "Drone", Description = "4k video", Price = 60.00m},
    new() { Name = "Shovel", Description = "Titanium steel", Price = 30.00m},
};

    public async Task<List<DemoItem>> GetDemoItemsAsync()
        => await _http.GetFromJsonAsync<List<DemoItem>>("/api/demo-items") ?? new();

    public async Task InsertSubmissionAsync(string name, string description, decimal price)
    {
        var resp = await _http.PostAsJsonAsync("/api/submissions", new { name, description, price });
        resp.EnsureSuccessStatusCode();
    }

    public async Task<DemoItem> InsertDemoItemAsync(string name, string description, decimal price)
    {
        var resp = await _http.PostAsJsonAsync("/api/demo-items", new { name, description, price });
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<DemoItem>() ?? new DemoItem
        {
            Name = name,
            Description = description,
            Price = price
        };
    }
}
