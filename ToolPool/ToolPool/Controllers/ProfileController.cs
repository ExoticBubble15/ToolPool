using Microsoft.AspNetCore.Mvc;
using ToolPool.Services;

namespace ToolPool.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    private readonly SupabaseDemoService _supabaseDemoService;

    public ProfileController(Supabase.Client supabase, SupabaseDemoService supabaseDemoService)
    {
        _supabase = supabase;
        _supabaseDemoService = supabaseDemoService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = TryGetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new { error = "No active user session." });
        }

        var me = await _supabaseDemoService.GetProfileUserAsync(userId.Value);
        if (me is null)
        {
            return NotFound(new { error = "Profile not found." });
        }

        return Ok(me);
    }

    [HttpGet("my-listings")]
    public async Task<IActionResult> GetMyListings()
    {
        var userId = TryGetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new { error = "No active user session." });
        }

        var listings = await _supabaseDemoService.GetListingsByOwnerAsync(userId.Value);
        return Ok(listings);
    }

    [HttpGet("my-activities")]
    public async Task<IActionResult> GetMyActivities()
    {
        var userId = TryGetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new { error = "No active user session." });
        }

        var activities = await _supabaseDemoService.GetActivitiesByUserAsync(userId.Value);
        return Ok(activities);
    }

    private Guid? TryGetCurrentUserId()
    {
        var userId = _supabase.Auth.CurrentSession?.User?.Id;
        if (Guid.TryParse(userId, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
