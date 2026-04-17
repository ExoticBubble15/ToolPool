using Microsoft.AspNetCore.Mvc;
using ToolPool.Client.Models;
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

    // =========================
    // CHECKOUT (existing)
    // =========================
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] List<CartItem> items)
    {
        if (items.Count == 0)
            return BadRequest("Cart is empty.");

        var total = items.Sum(i => i.Price);
        var destinationAccountId = await _supabase.GetStripeDestinationForToolAsync(items[0].DemoItemId);

        if (string.IsNullOrWhiteSpace(destinationAccountId))
            return BadRequest("Tool owner does not have a Stripe account.");

        var url = await _stripe.CreateCheckoutSessionAsync(items, total, destinationAccountId);

        return Ok(url);
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
