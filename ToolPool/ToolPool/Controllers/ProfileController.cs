using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolPool.Services;

namespace ToolPool.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly SupabaseDemoService _supabaseDemoService;
    private readonly UserService _userService;

    public ProfileController(SupabaseDemoService supabaseDemoService, UserService userService)
    {
        _supabaseDemoService = supabaseDemoService;
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = await TryGetCurrentUserIdAsync();
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
        var userId = await TryGetCurrentUserIdAsync();
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
        var userId = await TryGetCurrentUserIdAsync();
        if (userId is null)
        {
            return Unauthorized(new { error = "No active user session." });
        }

        var activities = await _supabaseDemoService.GetActivitiesByUserAsync(userId.Value);
        return Ok(activities);
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        var userId = await TryGetCurrentUserIdAsync();
        if (userId is null)
        {
            return Unauthorized(new { error = "No active user session." });
        }

        try
        {
            await _userService.DeleteAccountAsync(userId.Value);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { success = true, message = "Account deleted successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<Guid?> TryGetCurrentUserIdAsync()
    {
        var idClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("user_id");

        if (Guid.TryParse(idClaim, out var parsedFromClaim))
        {
            return parsedFromClaim;
        }

        var email =
            User.FindFirstValue(ClaimTypes.Email) ??
            User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var user = await _supabaseDemoService.GetUserAsync(email);
        if (user is null)
        {
            return null;
        }

        return user.Id;
    }
}
