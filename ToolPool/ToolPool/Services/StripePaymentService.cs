using Stripe.Checkout;
using ToolPool.Client.Models;

namespace ToolPool.Services;

public class StripePaymentService
{
    private readonly IHttpContextAccessor _http;

    public StripePaymentService(IHttpContextAccessor http)
    {
        _http = http;
    }

    public async Task<string> CreateCheckoutSessionAsync(List<CartItem> cartItems)
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
        };

        var session = await new SessionService().CreateAsync(options);
        return session.Url;
    }
}
