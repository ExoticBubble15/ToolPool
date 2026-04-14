using System.Net.Http.Json;
using ToolPool.Client.Models;

namespace ToolPool.Client.Services;

public class ToolService
{
    private readonly HttpClient _http;

    public ToolService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Tool>> GetToolsAsync()
        => await _http.GetFromJsonAsync<List<Tool>>("/api/Tools") ?? new();

    public async Task<Tool?> GetToolByIdAsync(Guid id)
    {
        var resp = await _http.GetAsync($"/api/Tools/{id}");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<Tool>();
    }

    public async Task<InterestResponse> SubmitInterestAsync(InterestRequest request)
    {
        var resp = await _http.PostAsJsonAsync("/api/interests", request);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<InterestResponse>() ?? new InterestResponse();
    }

    public async Task InsertSubmissionAsync(string name, string description, decimal price)
    {
        var resp = await _http.PostAsJsonAsync("/api/submissions", new { name, description, price });
        resp.EnsureSuccessStatusCode();
    }

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

    public async Task SeedToolsAsync()
    {
        var resp = await _http.PostAsync("/api/Tools/seed", null);
        resp.EnsureSuccessStatusCode();
    }
}
