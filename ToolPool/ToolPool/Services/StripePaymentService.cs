using BootstrapBlazor.Components;
using Stripe.Checkout;
using Stripe.Climate;
using ToolPool.Client.Models;
using Stripe;
namespace ToolPool.Services;

public class StripePaymentService
{
    private readonly IHttpContextAccessor _http;

    public StripePaymentService(IHttpContextAccessor http)
    {
        _http = http;
    }

    public async Task<string> CreateCheckoutSessionAsync(List<CartItem> cartItems, decimal total)
    {
        var req = _http.HttpContext!.Request;
        var baseUrl = $"{req.Scheme}://{req.Host}";

        var lineItems = cartItems.Select(item => new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = "usd",
                UnitAmountDecimal = (long) (item.Price * 100),
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = item.Name,
                },
            },
            Quantity = 1,
        }).ToList();

        var options = new SessionCreateOptions
        {
            LineItems = lineItems,
            Mode = "payment",
            BillingAddressCollection = "auto",
            SuccessUrl = $"{baseUrl}/success",
            CancelUrl = $"{baseUrl}/stripe",
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                ApplicationFeeAmount = (long)(total * 0.10m * 100), // 10% to ToolPool
                TransferData = new SessionPaymentIntentDataTransferDataOptions
                {
                    Destination = "acct_1TDv6d2OWxbeQ4IJ" // hardcoded test account
                }
            },
        };

        var session = await new SessionService().CreateAsync(options);

        return session.Url;
    }
}
