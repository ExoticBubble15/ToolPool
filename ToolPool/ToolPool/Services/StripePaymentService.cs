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

    public async Task<string> CreateCustomerAsync(string email)
    {
        var service = new CustomerService();

        var customer = await service.CreateAsync(new CustomerCreateOptions
        {
            Email = email,
        });

        return customer.Id;
    }

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
