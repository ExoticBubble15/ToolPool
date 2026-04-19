using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolPool.Client.Models;
using ToolPool.Models;
using ToolPool.Services;
using Stripe;

namespace ToolPool.Controllers;

[ApiController]
[Route("api/stripe")]
public class StripeController : ControllerBase
{
    private readonly StripePaymentService _stripe;
    private readonly SupabaseDemoService _supabase;

    public StripeController(StripePaymentService stripe, SupabaseDemoService supabase)
    {
        _stripe = stripe;
        _supabase = supabase;
    }

    /**
     * Route for checkout.
     * Gets cart from request.
     * Returns url for stripe session
     */
    [HttpPost("checkout-rental")]
    public async Task<IActionResult> CheckoutRental([FromBody] ToolPool.Models.StripeRentalRequest request)
    {
        var serverRequest = new ToolPool.Models.StripeRentalRequest
        {
            ToolId = request.ToolId,
            ToolName = request.ToolName,
            PricePerDay = request.PricePerDay,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            UserId = request.UserId,
            Message = request.Message
        };

        var url = await _stripe.CreateCheckoutSessionAsync(serverRequest);
        return Ok(new { url });
    }

    [HttpGet("chat-payment-context/{interestId:guid}")]
    public async Task<IActionResult> GetChatPaymentContext(Guid interestId)
    {
        // 1. Get current user from auth
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Not authenticated" });

        // 2. Look up interest by ID (safe, unique key)
        var interest = await _supabase.GetInterestByIdAsync(interestId);
        if (interest is null)
            return Ok(new { can_pay = false, reason = "Interest not found" });

        // 3. Verify current user is the renter
        if (interest.RenterId != userId.ToString())
            return Ok(new { can_pay = false, reason = "Only the renter can pay" });

        // 4. Load tool for price
        var tool = await _supabase.GetToolByIdAsync(interest.ToolId);
        if (tool is null)
            return Ok(new { can_pay = false, reason = "Tool not found" });

        // 5. Check dates
        if (string.IsNullOrEmpty(interest.StartDate) || string.IsNullOrEmpty(interest.EndDate))
            return Ok(new { can_pay = false, reason = "Rental dates not specified" });

        // 6. Calculate total
        DateTime.TryParse(interest.StartDate, out var startDate);
        DateTime.TryParse(interest.EndDate, out var endDate);
        var days = Math.Max(1, (endDate - startDate).Days + 1);
        var total = tool.Price * days;

        return Ok(new
        {
            can_pay = true,
            tool_name = interest.ToolName,
            price_per_day = tool.Price,
            start_date = interest.StartDate,
            end_date = interest.EndDate,
            total_amount = total
        });
    }

    [HttpPost("checkout-from-chat/{interestId:guid}")]
    public async Task<IActionResult> CheckoutFromChat(Guid interestId)
    {
        // 1. Get current user from auth
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Not authenticated" });

        // 2. Look up interest by ID
        var interest = await _supabase.GetInterestByIdAsync(interestId);
        if (interest is null)
            return BadRequest(new { error = "Interest not found" });

        // 3. Verify current user is the renter
        if (interest.RenterId != userId.ToString())
            return Forbid();

        // 4. Load tool for price
        var tool = await _supabase.GetToolByIdAsync(interest.ToolId);
        if (tool is null)
            return BadRequest(new { error = "Tool not found" });

        // 5. Validate dates
        if (string.IsNullOrEmpty(interest.StartDate) || string.IsNullOrEmpty(interest.EndDate))
            return BadRequest(new { error = "Rental dates not specified" });

        DateTime.TryParse(interest.StartDate, out var startDate);
        DateTime.TryParse(interest.EndDate, out var endDate);

        // 6. Create Stripe checkout
        var request = new ToolPool.Models.StripeRentalRequest
        {
            ToolId = interest.ToolId,
            ToolName = interest.ToolName,
            PricePerDay = tool.Price,
            StartDate = startDate,
            EndDate = endDate,
            UserId = userId,
            Message = interest.Message
        };

        var url = await _stripe.CreateCheckoutSessionAsync(request);
        return Ok(new { url });
    }

    // =========================
    // STRIPE ONBOARDING STATUS
    // =========================
    [HttpGet("onboarding-status/{accountId}")]
    public async Task<IActionResult> GetOnboardingStatus(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            return BadRequest("Missing Stripe account id");

        var service = new AccountService();

        try
        {
            var account = await service.GetAsync(accountId);

            var hasNoPendingRequirements =
                account.Requirements?.CurrentlyDue == null ||
                account.Requirements.CurrentlyDue.Count == 0;

            var isNotDisabled =
                string.IsNullOrEmpty(account.Requirements?.DisabledReason);

            var isOnboarded =
                account.DetailsSubmitted &&
                account.ChargesEnabled &&
                account.PayoutsEnabled &&
                hasNoPendingRequirements &&
                isNotDisabled;

            return Ok(isOnboarded);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Stripe error: {ex.Message}");
        }
    }

    [HttpPost("create-onboarding-link/{accountId}")]
    public IActionResult CreateOnboardingLink(string accountId)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var options = new AccountLinkCreateOptions
        {
            Account = accountId,
            RefreshUrl = $"{baseUrl}/stripe",
            ReturnUrl = $"{baseUrl}/stripe",
            Type = "account_onboarding"
        };

        var service = new AccountLinkService();
        var link = service.Create(options);

        return Ok(link.Url);
    }
}
