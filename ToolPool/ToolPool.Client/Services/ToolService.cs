using System.Net.Http.Json;
using System.Text.Json;
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

    public async Task<List<MarkerDetails>> GetMarkerDetails()
        => await _http.GetFromJsonAsync<List<MarkerDetails>>("/api/markerDetails") ?? new();
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

    public async Task<Tool> InsertToolAsync(Tool t)
    {
        var resp = await _http.PostAsJsonAsync("/api/Tools", t);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<Tool>() ?? new Tool();
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

    public async Task<PickupAddressInfo?> GetPickupAddressInfoAsync(Guid interestId)
    {
        // RentalStatus uses this to load the current address visibility and action buttons.
        var resp = await _http.GetAsync($"/api/interests/{interestId}/pickup-address");
        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException(await ReadErrorAsync(resp, "Failed to load pickup address."));
        }

        return await resp.Content.ReadFromJsonAsync<PickupAddressInfo>();
    }

    public async Task<PickupAddressInfo?> RevealPickupAddressAsync(Guid interestId)
    {
        // Owner action: move the rental to address revealed, if the server allows it.
        var resp = await _http.PostAsync($"/api/interests/{interestId}/reveal-address", null);
        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException(await ReadErrorAsync(resp, "Failed to reveal pickup address."));
        }

        return await resp.Content.ReadFromJsonAsync<PickupAddressInfo>();
    }

    public async Task<PickupAddressInfo?> StartHandoffAsync(Guid interestId)
    {
        // Owner action: starts the pickup handoff after the address is visible.
        var resp = await _http.PostAsync($"/api/interests/{interestId}/start-handoff", null);
        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException(await ReadErrorAsync(resp, "Failed to start handoff."));
        }

        return await resp.Content.ReadFromJsonAsync<PickupAddressInfo>();
    }

    public async Task<PickupAddressInfo?> ConfirmPickupAsync(Guid interestId)
    {
        // Renter action: confirms they received the tool.
        var resp = await _http.PostAsync($"/api/interests/{interestId}/confirm-pickup", null);
        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException(await ReadErrorAsync(resp, "Failed to confirm pickup."));
        }

        return await resp.Content.ReadFromJsonAsync<PickupAddressInfo>();
    }

    public async Task<PickupAddressInfo?> RequestReturnAsync(Guid interestId)
    {
        // Renter action: tells the owner the tool is ready to return.
        var resp = await _http.PostAsync($"/api/interests/{interestId}/request-return", null);
        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException(await ReadErrorAsync(resp, "Failed to request return."));
        }

        return await resp.Content.ReadFromJsonAsync<PickupAddressInfo>();
    }

    public async Task<PickupAddressInfo?> ConfirmReturnAsync(Guid interestId)
    {
        // Owner action: finishes the rental after the tool is back.
        var resp = await _http.PostAsync($"/api/interests/{interestId}/confirm-return", null);
        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException(await ReadErrorAsync(resp, "Failed to confirm return."));
        }

        return await resp.Content.ReadFromJsonAsync<PickupAddressInfo>();
    }

    public async Task<PickupAddressInfo?> SubmitOwnerRatingAsync(Guid interestId, int score)
    {
        // Renter action after completion: rate the owner for this rental.
        var resp = await _http.PostAsJsonAsync($"/api/interests/{interestId}/rating", new { score });
        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException(await ReadErrorAsync(resp, "Failed to submit rating."));
        }

        return await resp.Content.ReadFromJsonAsync<PickupAddressInfo>();
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, string fallback)
    {
        var error = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(error))
        {
            return fallback;
        }

        try
        {
            using var json = JsonDocument.Parse(error);
            if (json.RootElement.TryGetProperty("error", out var errorValue))
            {
                var parsed = errorValue.GetString();
                if (!string.IsNullOrWhiteSpace(parsed))
                {
                    return parsed;
                }
            }
        }
        catch
        {
        }

        return error;
    }
}
