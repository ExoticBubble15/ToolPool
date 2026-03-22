using Microsoft.AspNetCore.Mvc;
using ToolPool.Client.Models;
using ToolPool.Services;

namespace ToolPool.Controllers;

[ApiController]
[Route("api/stripe")]
public class StripeController : ControllerBase
{
    private readonly StripePaymentService _stripe;

    public StripeController(StripePaymentService stripe)
    {
        _stripe = stripe;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] List<CartItem> items)
    {
        var url = await _stripe.CreateCheckoutSessionAsync(items);
        return Ok(url);
    }
}