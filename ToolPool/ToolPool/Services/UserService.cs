using Microsoft.AspNetCore.Identity.Data;
using ToolPool.Services;
using ToolPool.Models

public class UserService
{
    private readonly SupabaseDemoService _db;
    private readonly StripePaymentService _stripe;

    public UserService(SupabaseDemoService db, StripePaymentService stripe)
    {
        _db = db;
        _stripe = stripe;
    }

    public async Task<User> RegisterUserAsync(RegisterRequest request)
    {
        // Create user in Supabase
        // Todo

        // Create Stripe Customer
        var customerId = _stripe.CreateCustomer(request.Email);

        // Create Stripe Seller Account
        var accountId = _stripe.CreateConnectedAccount(request.Email);

        // 4. Save to Supabase
        // Todo

    }
}