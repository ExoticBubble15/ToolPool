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
    private readonly ILogger<SendbirdController> _logger;

    public SendbirdController(SendbirdService sendbird, SupabaseDemoService supabase, ILogger<SendbirdController> logger)
    {
        _sendbird = sendbird;
        _supabase = supabase;
        _logger = logger;
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

        // Intentional read-with-side-effect: self-heal provisioning for legacy users
        // who never got a Sendbird account at signup. This keeps the chat UI free of
        // a separate onboarding step.
        string sendbirdId;
        try
        {
            var desired = string.IsNullOrEmpty(user.SendbirdUserId) ? user.Id.ToString() : user.SendbirdUserId;
            sendbirdId = await _sendbird.CreateOrGetUserAsync(desired, user.Username ?? desired);
            if (user.SendbirdUserId != sendbirdId)
                await _supabase.UpdateUserSendbirdIdAsync(user.Id, sendbirdId);
        }
        catch (SendbirdException ex)
        {
            _logger.LogError(ex, "Sendbird user provisioning failed in channels/me for user {UserId}", userId);
            return StatusCode(502, new { error = "Chat provisioning failed" });
        }

        var channels = await _sendbird.ListUserChannelsAsync(sendbirdId);
        return Ok(channels);
    }

    public record CreateChannelRequest(string RenterId, string OwnerId, string ItemName);
}
