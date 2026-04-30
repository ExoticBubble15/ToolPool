/**
 * Backend Service for Stripe API
 */

using GoogleMapsComponents.Maps.Coordinates;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using ToolPool.Client.Models;
using ToolPool.Models;
namespace ToolPool.Services;

/// <summary>
/// Provides methods for integrating with Stripe to manage customers, connected accounts, checkout sessions, and
/// transfers for payment processing.
/// </summary>
/// <remarks>This service encapsulates common Stripe payment operations such as creating customers, onboarding
/// connected accounts, initiating checkout sessions for rentals, and transferring payouts to owners.</remarks>
public class StripePaymentService
{
    private readonly AccountService _accountService = new();
    private readonly SupabaseDemoService _supabaseService;




    // provides access to current http request/response
    private readonly IHttpContextAccessor _http;

    // constructor
    public StripePaymentService(IHttpContextAccessor http, SupabaseDemoService supabaseService)
    {
        _http = http;
        _supabaseService = supabaseService;
    }

    /// <summary>
    /// Creates a new customer with the specified email address asynchronously.
    /// </summary>
    /// <param name="email">The email address to associate with the new customer. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the unique identifier of the created
    /// customer.</returns>
    public async Task<string> CreateCustomerAsync(string email)
    {
        var service = new CustomerService();

        var customer = await service.CreateAsync(new CustomerCreateOptions
        {
            Email = email,
        });

        return customer.Id;
    }

    /// <summary>
    /// Creates a new connected account using the specified email address and returns the account identifier.
    /// </summary>
    /// <param name="email">The email address to associate with the new connected account. Cannot be null or empty.</param>
    /// <returns>A string containing the unique identifier of the newly created connected account.</returns>
    public async Task<string> CreateConnectedAccountAsync(string email)
    {
        var service = new AccountService();

        var account = await service.CreateAsync(new AccountCreateOptions
        {
            Type = "express",
            Email = email
        });

        return account.Id;
    }

    /// <summary>
    /// Determines whether the user associated with the specified Stripe account has completed all onboarding
    /// requirements and is fully enabled for payments and payouts.
    /// </summary>
    /// <remarks>A user is considered fully onboarded if all required account details have been submitted,
    /// there are no pending requirements, and the account is not disabled for any reason. This method is typically used
    /// to verify eligibility before allowing payment or payout operations.</remarks>
    /// <param name="stripeAccountId">The unique identifier of the Stripe account to check. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if the user
    /// is fully onboarded and the account is enabled for charges and payouts; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> IsUserFullyOnboardedAsync(string stripeAccountId)
    {
        var account = await _accountService.GetAsync(stripeAccountId);

        var hasNoPendingRequirements =
            account.Requirements?.CurrentlyDue == null ||
            account.Requirements.CurrentlyDue.Count == 0;

        var isNotDisabled =
            string.IsNullOrEmpty(account.Requirements?.DisabledReason);

        return account.DetailsSubmitted
            && account.ChargesEnabled
            && account.PayoutsEnabled
            && hasNoPendingRequirements
            && isNotDisabled;
    }

    // async method to create a stripe checkout session
    public async Task<string> CreateCheckoutSessionAsync(ToolPool.Models.StripeRentalRequest request)
    {
        var req = _http.HttpContext!.Request;
        var baseUrl = $"{req.Scheme}://{req.Host}";
        Console.WriteLine($"OwnerId being used: {request.OwnerId}");
        var owner = await _supabaseService.GetUserByIdAsync(request.OwnerId);

        var days = (request.EndDate.Date - request.StartDate.Date).Days + 1;
        if (days <= 0) throw new Exception("Invalid date range");

        var total = request.PricePerDay * days;

        // options for the session including metadata to pass through for creating an interest item after payment success
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            CustomerEmail = request.UserEmail,
            Metadata = new Dictionary<string, string>
{
    { "UserId", request.UserId.ToString() },
    { "ToolId", request.ToolId.ToString() },
    { "ToolName", request.ToolName },
    { "Message", request.Message ?? "" },
    { "StartDate", request.StartDate.ToString("yyyy-MM-dd") },
    { "EndDate", request.EndDate.ToString("yyyy-MM-dd") }
},

            LineItems = new List<SessionLineItemOptions>
    {
        new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = "usd",
                UnitAmount = (long)(request.PricePerDay * 100),
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = $"{request.ToolName} ({days} days)"
                }
            },
            Quantity = days
        }
    },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                TransferData = new SessionPaymentIntentDataTransferDataOptions
                {
                    Destination = owner.StripeAccountId
                }
            },

            SuccessUrl = $"{baseUrl}/payment/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{baseUrl}/express_interest/{request.ToolId}"
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return session.Url;
    }

    /// <summary>
    /// Transfers the specified amount to the owner's Stripe account as a payout.
    /// </summary>
    /// <remarks>The amount is converted from dollars to cents before initiating the transfer. The transfer is
    /// processed in USD currency.</remarks>
    /// <param name="ownerStripeAccountId">The Stripe account ID of the owner to receive the payout. Cannot be null or empty.</param>
    /// <param name="amount">The amount, in US dollars, to transfer to the owner's account. Must be greater than zero.</param>
    /// <returns>A task that represents the asynchronous transfer operation.</returns>
    public async Task TransferToOwnerAsync(string ownerStripeAccountId, decimal amount)
    {
        var transferService = new TransferService();

        var options = new TransferCreateOptions
        {
            Amount = (long)(amount * 100), // dollars → cents
            Currency = "usd",
            Destination = ownerStripeAccountId,
            Description = "Tool rental payout"
        };

        await transferService.CreateAsync(options);
    }
}
