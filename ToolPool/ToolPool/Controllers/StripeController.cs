using Microsoft.AspNetCore.Mvc;
using ToolPool.Client.Models;
using ToolPool.Models;
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
}