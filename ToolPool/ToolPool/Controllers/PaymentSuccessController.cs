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

    public PaymentController(
        SupabaseDemoService supabase,
        SendbirdService sendbird)
    {
        _supabase = supabase;
        _sendbird = sendbird;
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
            // 5. Ensure Sendbird IDs
            // -----------------------------
            if (string.IsNullOrEmpty(renter.SendbirdUserId))
            {
                renter.SendbirdUserId = renter.Id.ToString();
                await _supabase.UpdateUserSendbirdIdAsync(renter.Id, renter.SendbirdUserId);
            }

            if (string.IsNullOrEmpty(owner.SendbirdUserId))
            {
                owner.SendbirdUserId = owner.Id.ToString();
                await _supabase.UpdateUserSendbirdIdAsync(owner.Id, owner.SendbirdUserId);
            }

            // -----------------------------
            // 6. Create chat (non-fatal)
            // -----------------------------
            string? channelUrl = null;

            try
            {
                var channel = await _sendbird.CreateGroupChannelAsync(
                    renter.SendbirdUserId,
                    owner.SendbirdUserId,
                    tool.Name
                );

                channelUrl = channel.ChannelUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sendbird error (ignored): {ex.Message}");
            }

            // -----------------------------
            // 7. Check for existing interest (avoid duplicates)
            // -----------------------------
            var existingInterest = await _supabase.GetInterestByRenterAndToolAsync(renter.Id.ToString(), toolId);

            if (existingInterest != null)
            {
                Console.WriteLine($"FOUND EXISTING INTEREST: {existingInterest.Id}");
                // Reuse existing interest and channel - payment just confirms it
                var existingChannelUrl = existingInterest.ChannelUrl ?? channelUrl;
                return Ok(new InterestResponse
                {
                    Success = true,
                    ChannelUrl = existingChannelUrl,
                    InterestId = existingInterest.Id
                });
            }

            // -----------------------------
            // 8. Create new interest record (only if none exists)
            // -----------------------------
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

            Console.WriteLine("ABOUT TO INSERT INTEREST");

            var saved = await _supabase.InsertInterestAsync(interest);

            if (saved is null)
            {
                Console.WriteLine("INSERT FAILED (null result)");
                return StatusCode(500, "Interest insert failed");
            }

            Console.WriteLine($"INTEREST CREATED: {saved.Id}");

            // -----------------------------
            // 9. Return response
            // -----------------------------
            return Ok(new InterestResponse
            {
                Success = true,
                ChannelUrl = channelUrl,
                InterestId = saved.Id
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 PAYMENT SUCCESS ERROR: {ex}");
            return StatusCode(500, ex.Message);
        }
    }
}