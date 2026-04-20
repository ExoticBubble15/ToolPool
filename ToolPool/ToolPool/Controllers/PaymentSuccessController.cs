using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe;
using ToolPool.Models;
using ToolPool.Services;

namespace ToolPool.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly SupabaseDemoService _supabase;
    private readonly SendbirdService _sendbird;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        SupabaseDemoService supabase,
        SendbirdService sendbird,
        ILogger<PaymentController> logger)
    {
        _supabase = supabase;
        _sendbird = sendbird;
        _logger = logger;
    }

    [HttpGet("success")]
    public async Task<IActionResult> Success([FromQuery] string session_id)
    {
        try
        {
            // -----------------------------
            // 1. Get Stripe session
            // -----------------------------
            var sessionService = new SessionService();

            var session = await sessionService.GetAsync(session_id, new SessionGetOptions
            {
                Expand = new List<string> { "payment_intent" }
            });

            // -----------------------------
            // 2. Get metadata safely
            // -----------------------------
            var md = session.Metadata;

            // fallback to PaymentIntent metadata (IMPORTANT FIX)
            if (md == null || md.Count == 0)
            {
                var pi = session.PaymentIntent as PaymentIntent;
                md = pi?.Metadata ?? new Dictionary<string, string>();
            }

            Console.WriteLine("FINAL METADATA:");
            foreach (var kv in md)
                Console.WriteLine($"{kv.Key} = {kv.Value}");

            // -----------------------------
            // 3. Safe parsing helpers
            // -----------------------------
            if (!md.TryGetValue("UserId", out var userIdRaw) ||
                !Guid.TryParse(userIdRaw, out var renterId))
            {
                Console.WriteLine("Missing/invalid UserId");
                return BadRequest("Missing renter identity");
            }

            if (!md.TryGetValue("ToolId", out var toolIdRaw) ||
                !Guid.TryParse(toolIdRaw, out var toolId))
            {
                return BadRequest("Missing ToolId");
            }

            md.TryGetValue("ToolName", out var toolName);
            md.TryGetValue("Message", out var message);
            md.TryGetValue("StartDate", out var startDateRaw);
            md.TryGetValue("EndDate", out var endDateRaw);

            DateTime.TryParse(startDateRaw, out var startDate);
            DateTime.TryParse(endDateRaw, out var endDate);

            // -----------------------------
            // 4. Load users + tool
            // -----------------------------
            var renter = await _supabase.GetUserByIdAsync(renterId);
            if (renter is null)
                return BadRequest("User not found");

            var tool = await _supabase.GetToolByIdAsync(toolId);
            if (tool is null)
                return NotFound("Tool not found");

            if (tool.OwnerId is null)
                return BadRequest("Tool has no owner");

            if (tool.OwnerId == renter.Id)
                return BadRequest("Cannot rent your own tool");

            var owner = await _supabase.GetUserByIdAsync(tool.OwnerId.Value);
            if (owner is null)
                return BadRequest("Owner not found");

            // -----------------------------
            // 5. Ensure Sendbird users exist (provision + persist id). Hard fail if down.
            // -----------------------------
            string renterSbId, ownerSbId;
            try
            {
                renterSbId = await EnsureSendbirdUserAsync(renter);
                ownerSbId = await EnsureSendbirdUserAsync(owner);
            }
            catch (SendbirdException ex)
            {
                _logger.LogError(ex, "Sendbird user provisioning failed in PaymentSuccess (renter={RenterId}, owner={OwnerId})", renter.Id, owner.Id);
                return StatusCode(502, "Chat provisioning failed");
            }

            // -----------------------------
            // 6. Check for existing interest (dedupe on (renter, tool))
            // -----------------------------
            var existingInterest = await _supabase.GetInterestByRenterAndToolAsync(renter.Id.ToString(), toolId);

            if (existingInterest != null)
            {
                // Verify and self-heal the existing channel. Historical rows from
                // the old is_distinct era may share a channel across tools — if
                // the renter is not a member of the stored channel, we create a
                // fresh per-interest channel and update the row.
                string reusedChannelUrl;
                try
                {
                    if (!string.IsNullOrEmpty(existingInterest.ChannelUrl)
                        && await _sendbird.VerifyChannelMembersAsync(existingInterest.ChannelUrl, renterSbId, ownerSbId))
                    {
                        reusedChannelUrl = existingInterest.ChannelUrl;
                    }
                    else
                    {
                        var refreshed = await _sendbird.CreateGroupChannelAsync(
                            renterSbId, ownerSbId, tool.Name, interestId: existingInterest.Id.ToString());
                        reusedChannelUrl = refreshed.ChannelUrl;
                        await _supabase.UpdateInterestChannelUrlAsync(existingInterest.Id, reusedChannelUrl);
                        _logger.LogInformation("Self-healed channel_url for interest {InterestId}: {NewUrl}", existingInterest.Id, reusedChannelUrl);
                    }
                }
                catch (SendbirdException ex)
                {
                    _logger.LogError(ex, "Sendbird channel verify/create failed for existing interest {InterestId}", existingInterest.Id);
                    return StatusCode(502, "Chat provisioning failed");
                }

                return Ok(new InterestResponse
                {
                    Success = true,
                    ChannelUrl = reusedChannelUrl,
                    InterestId = existingInterest.Id
                });
            }

            // -----------------------------
            // 7. No existing interest — create fresh per-interest channel + row
            // -----------------------------
            string channelUrl;
            try
            {
                var channel = await _sendbird.CreateGroupChannelAsync(renterSbId, ownerSbId, tool.Name);
                channelUrl = channel.ChannelUrl;
            }
            catch (SendbirdException ex)
            {
                _logger.LogError(ex, "Sendbird channel creation failed in PaymentSuccess (renter={RenterId}, tool={ToolId})", renter.Id, toolId);
                return StatusCode(502, "Chat provisioning failed");
            }

            var interest = new InterestSubmission
            {
                ToolId = toolId,
                ToolName = toolName ?? tool.Name,
                RenterId = renter.Id.ToString(),
                OwnerId = tool.OwnerId,
                Message = message,
                StartDate = startDate.ToString(),
                EndDate = endDate.ToString(),
                ChannelUrl = channelUrl,
                Status = "pending"
            };

            var saved = await _supabase.InsertInterestAsync(interest);

            if (saved is null)
            {
                _logger.LogError("Interest insert returned null (channelUrl={ChannelUrl})", channelUrl);
                return StatusCode(500, "Interest insert failed");
            }

            return Ok(new InterestResponse
            {
                Success = true,
                ChannelUrl = channelUrl,
                InterestId = saved.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PAYMENT SUCCESS unhandled error");
            return StatusCode(500, ex.Message);
        }
    }

    private async Task<string> EnsureSendbirdUserAsync(Models.AppUser user)
    {
        var desired = string.IsNullOrEmpty(user.SendbirdUserId) ? user.Id.ToString() : user.SendbirdUserId;
        var provisioned = await _sendbird.CreateOrGetUserAsync(desired, user.Username ?? desired);
        if (user.SendbirdUserId != provisioned)
        {
            await _supabase.UpdateUserSendbirdIdAsync(user.Id, provisioned);
            user.SendbirdUserId = provisioned;
        }
        return provisioned;
    }
}
