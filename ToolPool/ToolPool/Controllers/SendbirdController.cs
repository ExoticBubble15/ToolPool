using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolPool.Services;

namespace ToolPool.Controllers;

[ApiController]
[Route("api/sendbird")]
public class SendbirdController : ControllerBase
{
    private readonly SendbirdService _sendbird;
    private readonly SupabaseDemoService _supabase;

    public SendbirdController(SendbirdService sendbird, SupabaseDemoService supabase)
    {
        _sendbird = sendbird;
        _supabase = supabase;
    }

    [HttpGet("app-id")]
    public ActionResult<string> GetAppId()
    {
        return Ok(_sendbird.GetAppId());
    }

    [HttpPost("channel")]
    public async Task<IActionResult> CreateChannel([FromBody] CreateChannelRequest request)
    {
        var channel = await _sendbird.CreateGroupChannelAsync(
            request.RenterId, request.OwnerId, request.ItemName);
        return Ok(channel);
    }

    [HttpGet("channels/{userId}")]
    public async Task<IActionResult> ListChannels(string userId)
    {
        var channels = await _sendbird.ListUserChannelsAsync(userId);
        return Ok(channels);
    }

    /// <summary>
    /// Lists channels for the authenticated user using their sendbird_user_id.
    /// Clients should use this instead of passing userId manually.
    /// </summary>
    [HttpGet("channels/me")]
    public async Task<IActionResult> ListMyChannels()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            return Unauthorized(new { error = "Not authenticated" });

        var user = await _supabase.GetUserByIdAsync(userId);
        if (user is null)
            return Unauthorized(new { error = "User not found" });

        var sendbirdId = user.SendbirdUserId ?? user.Id.ToString();
        var channels = await _sendbird.ListUserChannelsAsync(sendbirdId);
        return Ok(channels);
    }

    public record CreateChannelRequest(string RenterId, string OwnerId, string ItemName);
}
