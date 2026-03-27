using Microsoft.AspNetCore.Mvc;
using ToolPool.Services;

namespace ToolPool.Controllers;

[ApiController]
[Route("api/sendbird")]
public class SendbirdController : ControllerBase
{
    private readonly SendbirdService _sendbird;

    public SendbirdController(SendbirdService sendbird)
    {
        _sendbird = sendbird;
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

    public record CreateChannelRequest(string RenterId, string OwnerId, string ItemName);
}
