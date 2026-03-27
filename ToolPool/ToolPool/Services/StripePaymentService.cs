/**
 * Backend Service for Stripe API
 */

using Stripe.Checkout;
using Stripe.Climate;
using ToolPool.Client.Models;
using Stripe;
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
    public async Task<string> CreateCheckoutSessionAsync(List<CartItem> cartItems, decimal total)
    {
        
        // request obj
        var req = _http.HttpContext!.Request;

        // url for toolpool
        var baseUrl = $"{req.Scheme}://{req.Host}";

        // create line items from the items in the cart
        var lineItems = cartItems.Select(item => new SessionLineItemOptions
        {
            // pricing data for each item
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = "usd",
                UnitAmountDecimal = (long) (item.Price * 100),
                // meta info for each item
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = item.Name,
                },
            },
            Quantity = 1,
        }).ToList();

        // customiziation for the session
        var options = new SessionCreateOptions
        {   
            LineItems = lineItems,
            Mode = "payment",
            BillingAddressCollection = "auto",
            SuccessUrl = $"{baseUrl}/success",
            CancelUrl = $"{baseUrl}/stripe",

            // payment intent tracks payment flow
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                ApplicationFeeAmount = (long)(total * 0.10m * 100), // 10% to ToolPool

                // TransferData to send money to another account
                TransferData = new SessionPaymentIntentDataTransferDataOptions
                {
                    Destination = "acct_1TDv6d2OWxbeQ4IJ" // hardcoded test account
                }
            },
        };

        // start the new session
        var session = await new SessionService().CreateAsync(options);

        return session.Url; 
    }
}
