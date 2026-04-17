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
    // provides access to current http request/response
    private readonly IHttpContextAccessor _http;

    // constructor
    public StripePaymentService(IHttpContextAccessor http)
    {
        _http = http;
    }

    // async method to create a stripe checkout session
    public async Task<string> CreateCheckoutSessionAsync(ToolPool.Models.StripeRentalRequest request)
    {
        var req = _http.HttpContext!.Request;
        var baseUrl = $"{req.Scheme}://{req.Host}";

        var days = (request.EndDate.Date - request.StartDate.Date).Days + 1;
        if (days <= 0) throw new Exception("Invalid date range");

        var total = request.PricePerDay * days;

        var options = new SessionCreateOptions
        {
            Mode = "payment",

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

            SuccessUrl = $"{baseUrl}/payment/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{baseUrl}/express_interest/{request.ToolId}"
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return session.Url;
    }
}
