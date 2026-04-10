/**
 * Service to manage catalog
 */

using System.Net.Http.Json;
using ToolPool.Client.Models;

namespace ToolPool.Client.Services;

public class ToolService
{
    // HttpClient for API requests
    private readonly HttpClient _http;

    public ToolService(HttpClient http)
    {
        _http = http;
    }

    // Fetches all Tools from the API
    // Returns an empty list if the response is null
    public async Task<List<Tool>> GetToolsAsync()
        => await _http.GetFromJsonAsync<List<Tool>>("/api/Tools") ?? new();

    // Submits a new item suggestion/submission to the API (does not return the created item)
    public async Task InsertSubmissionAsync(string name, string description, decimal price)
    {
        var resp = await _http.PostAsJsonAsync("/api/submissions", new { name, description, price });
        resp.EnsureSuccessStatusCode();
    }

    // Posts a new DemoItem to the API and returns the created item
    // Returns locally created DemoItem if the API response body is null
    public async Task<Tool> InsertToolAsync(string name, string description, decimal price)
    {
        var resp = await _http.PostAsJsonAsync("/api/Tools", new { name, description, price });
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<Tool>() ?? new Tool
        {
            Name = name,
            Description = description,
            Price = price
        };
    }

    public async Task DeleteToolAsync(Guid id)
    {
        var resp = await _http.DeleteAsync($"api/Tools/{id}");
        resp.EnsureSuccessStatusCode();
    }
}