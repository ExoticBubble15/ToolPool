/**
 * Service to manage catalog
 */

using System.Net.Http.Json;
using ToolPool.Client.Models;

namespace ToolPool.Client.Services;

public class DemoItemService
{
    // HttpClient for API requests
    private readonly HttpClient _http;

    public DemoItemService(HttpClient http)
    {
        _http = http;
    }

    // Returns a hardcoded list of demo items
    public List<DemoItem> GetItems() => new()
    {
        new() { Name = "Bike", Description = "It works", Price = 25.00m },
        new() { Name = "DSLR Camera", Description = "Canon EOS R5 with 3 lenses", Price = 80.00m},
        new() { Name = "Camping Tent", Description = " waterproof tent", Price = 20.00m},
        new() { Name = "Kayak", Description = "Single seat, paddle included", Price = 45.00m},
        new() { Name = "Drone", Description = "4k video", Price = 60.00m},
        new() { Name = "Shovel", Description = "Titanium steel", Price = 30.00m},
    };

    // Fetches all demo items from the API
    // Returns an empty list if the response is null
    public async Task<List<DemoItem>> GetDemoItemsAsync()
        => await _http.GetFromJsonAsync<List<DemoItem>>("/api/demo-items") ?? new();

    // Submits a new item suggestion/submission to the API (does not return the created item)
    public async Task InsertSubmissionAsync(string name, string description, decimal price)
    {
        var resp = await _http.PostAsJsonAsync("/api/submissions", new { name, description, price });
        resp.EnsureSuccessStatusCode();
    }

    // Posts a new DemoItem to the API and returns the created item
    // Returns locally created DemoItem if the API response body is null
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

    public async Task DeleteDemoItemAsync(Guid id)
    {
        var resp = await _http.DeleteAsync($"api/demo-items/{id}");
        resp.EnsureSuccessStatusCode();
    }
}