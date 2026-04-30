using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolPool.Client.Models;
using ToolPool.Models;
using ToolPool.Services;
using Stripe;

namespace ToolPool.Controllers;
/// <summary>
/// Provides API endpoints for handling Stripe payment operations.  
/// </summary>
/// <remarks>This controller exposes endpoints for integrating Stripe payments within the application. It supports
/// creating checkout sessions for rentals, validating payment eligibility, and managing Stripe account onboarding. All
/// endpoints require the user to be authenticated. The controller relies on injected services for Stripe payment
/// processing and Supabase data access.</remarks>
[ApiController]
[Route("api/stripe")]
public class StripeController : ControllerBase
{
    private readonly StripePaymentService _stripe;
    private readonly SupabaseDemoService _supabase;

    /// <summary>
    /// Initializes a new instance of the StripeController class with the specified payment and Supabase services.
    /// </summary>
    /// <param name="stripe">The StripePaymentService instance used to process Stripe payments. Cannot be null.</param>
    /// <param name="supabase">The SupabaseDemoService instance used to interact with Supabase features. Cannot be null.</param>
    public StripeController(StripePaymentService stripe, SupabaseDemoService supabase)
    {
        _stripe = stripe;
        _supabase = supabase;
    }

    /// <summary>
    /// Creates a Stripe checkout session for renting a tool based on the provided rental request.
    /// </summary>
    /// <remarks>The caller must be an authenticated user. The method validates that the tool exists, has an
    /// owner, and that the user is not attempting to rent their own tool.</remarks>
    /// <param name="request">The rental request containing tool information, rental period, price, and user details. Must not be null.</param>
    /// <returns>An IActionResult containing the URL for the Stripe checkout session if the request is valid; otherwise, a
    /// BadRequest or Unauthorized result describing the error.</returns>
    [HttpPost("checkout-rental")]
    public async Task<IActionResult> CheckoutRental([FromBody] ToolPool.Models.StripeRentalRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized(new { error = "Not authenticated" });

        var tool = await _supabase.GetToolByIdAsync(request.ToolId);
        if (tool is null)
            return BadRequest(new { error = "Tool not found" });

        if (!tool.OwnerId.HasValue)
            return BadRequest(new { error = "Tool owner is missing." });

        var ownerId = tool.OwnerId.Value;

        if (ownerId == currentUserId)
            return BadRequest(new { error = "You cannot pay for your own listing." });

        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        var serverRequest = new ToolPool.Models.StripeRentalRequest
        {
            ToolId = request.ToolId,
            ToolName = string.IsNullOrWhiteSpace(request.ToolName) ? tool.Name : request.ToolName,
            PricePerDay = request.PricePerDay,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            UserId = currentUserId,
            Message = request.Message,
            UserEmail = string.IsNullOrWhiteSpace(request.UserEmail) ? email : request.UserEmail,
            OwnerId = ownerId
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

        if (interest.OwnerId == userId)
            return Ok(new { can_pay = false, reason = "You cannot pay for your own listing" });

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

    /// <summary>
    /// Initiates the checkout process for a rental interest from a chat context and returns a Stripe checkout session
    /// URL if successful.
    /// </summary>
    /// <remarks>The current user must be authenticated and must be the renter associated with the specified
    /// interest. The method validates that the interest and associated tool exist, that rental dates are specified, and
    /// that the user is not attempting to pay for their own listing.</remarks>
    /// <param name="interestId">The unique identifier of the rental interest to check out. Must correspond to an existing interest where the
    /// current user is the renter.</param>
    /// <returns>An IActionResult containing the Stripe checkout session URL if the operation is successful; otherwise, an error
    /// response indicating the reason for failure.</returns>
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

        if (interest.OwnerId == userId)
            return BadRequest(new { error = "You cannot pay for your own listing." });

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

    /// <summary>
    /// Retrieves the onboarding status for the specified Stripe account.
    /// </summary>
    /// <remarks>A Stripe account is considered onboarded if all required details are submitted, charges and
    /// payouts are enabled, there are no pending requirements, and the account is not disabled.</remarks>
    /// <param name="accountId">The unique identifier of the Stripe account to check. Cannot be null, empty, or whitespace.</param>
    /// <returns>An HTTP 200 response containing a boolean value indicating whether the account is fully onboarded. Returns HTTP
    /// 400 if the account ID is missing or invalid, or HTTP 500 if an error occurs while retrieving the account status.</returns>
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


    /// <summary>
    /// Creates a Stripe onboarding link for the specified account and returns the URL to the client.
    /// </summary>
    /// <remarks>Use this endpoint to initiate the Stripe onboarding flow for a connected account. The
    /// returned URL should be presented to the user to complete onboarding. The link is available for a time determined by Stripe.</remarks>
    /// <param name="accountId">The unique identifier of the Stripe account for which to create the onboarding link. Cannot be null or empty.</param>
    /// <returns>An HTTP 200 response containing the URL for the Stripe onboarding process.</returns>
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
