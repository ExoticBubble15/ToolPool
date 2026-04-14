using Microsoft.AspNetCore.Mvc;
using ToolPool.Client.Models;
using ToolPool.Services;
/**
 * Controller for Stripe Service
 */


namespace ToolPool.Controllers;

[ApiController]
[Route("api/stripe")]
public class StripeController : ControllerBase
{   
    // service
    private readonly StripePaymentService _stripe;

    // constructor
    public StripeController(StripePaymentService stripe)
    {
        _stripe = stripe;
    }

    /**
     * Route for checkout.
     * Gets cart from request.
     * Returns url for stripe session
     */
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] List<CartItem> items)
    {
        var total = items.Sum(i => i.Price); // total price of cart

        var url = await _stripe.CreateCheckoutSessionAsync(items, total); // call create session method from service

        return Ok(url);
    }
}