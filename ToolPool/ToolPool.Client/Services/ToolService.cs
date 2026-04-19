using System.Net.Http.Json;
using ToolPool.Client.Models;
using ToolPool.Client.Pages;

namespace ToolPool.Client.Services;

public class ToolService
{
    private readonly HttpClient _http;

    public ToolService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<NeighborhoodTriple>> GetNeighborhoodTriples()
        => await _http.GetFromJsonAsync<List<NeighborhoodTriple>>("/api/neighborhoodTriples") ?? new();

    public async Task<String> ReverseGeocode(string latitude, string longitude)
        => await _http.GetStringAsync($"/api/reverseGeocode/{latitude}/{longitude}");

    public async Task<List<String>> GetCategoriesAsync()
        => await _http.GetFromJsonAsync<List<String>>("/api/categories") ?? new();

    public async Task<List<String>> GetNeighborhodsAsync()
        => await _http.GetFromJsonAsync<List<String>>("/api/neighborhoods") ?? new();

    //public async Task<Dictionary<String, List<String>>> GetCityNeighborhoodsAsync()
    //    => await _http.GetFromJsonAsync<Dictionary<String, List<String>>>("/api/cityNeighborhoods") ?? new();

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

    // ── Auth ──

    public async Task<AppUser?> GetCurrentUserAsync()
    {
        try
        {
            var resp = await _http.GetAsync("/api/auth/me");
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<AppUser>();
        }
        catch { return null; }
    }

    public async Task<AppUser?> DevLoginAsync(string identifier)
    {
        var resp = await _http.PostAsJsonAsync("/api/auth/dev-login", new { identifier });
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<AppUser>();
    }

    public async Task LogoutAsync()
    {
        await _http.PostAsync("/api/auth/logout", null);
    }

    // ── Interests ──

    public async Task<List<MyInterestItem>> GetMyInterestsAsync(Guid userId)
    {
        return await _http.GetFromJsonAsync<List<MyInterestItem>>(
            $"/api/my-interests?userId={userId}"
        ) ?? new();
    }
    // ── Chat Payment ──

    public async Task<ChatPaymentContext?> GetChatPaymentContextAsync(Guid interestId)
    {
        try
        {
            var resp = await _http.GetAsync($"/api/stripe/chat-payment-context/{interestId}");
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<ChatPaymentContext>();
        }
        catch { return null; }
    }

    public async Task<string?> CheckoutFromChatAsync(Guid interestId)
    {
        var resp = await _http.PostAsync($"/api/stripe/checkout-from-chat/{interestId}", null);
        if (!resp.IsSuccessStatusCode) return null;
        var result = await resp.Content.ReadFromJsonAsync<StripeResponse>();
        return result?.Url;
    }
}
