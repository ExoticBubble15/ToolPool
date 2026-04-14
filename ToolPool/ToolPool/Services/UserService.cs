using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity.Data;
using ToolPool.Services;
using ToolPool.Models;

public class UserService
{
    private readonly SupabaseDemoService _db;
    private readonly StripePaymentService _stripe;
    private readonly Supabase.Client _supabase;
    private readonly HttpClient _http;

    public UserService(SupabaseDemoService db, StripePaymentService stripe, Supabase.Client supabase, HttpClient http)
    {
        _db = db;
        _stripe = stripe;
        _supabase = supabase;
        _http = http;
    }

    public async Task<User> RegisterUserAsync(RegisterRequest request)
    {
        // Create user in Supabase
        var session = await _supabase.Auth.SignUp(request.Email, request.Password);
        // ADD USER ALREADY EXISTS LOGIC - !
        // Create Stripe Customer
        var customerId = _stripe.CreateCustomer(request.Email);
        // Create Stripe Seller Account
        var accountId = _stripe.CreateConnectedAccount(request.Email);
        // TODO: create sendbird id
        var sendbirdId = await sendbird.CreateOrGetUserAsync(request.Email, session?.User?.Id ?? "");
        
        
        // 4. Save to Supabase
        var newUser = new User
        {
            Username = session?.User?.Id ?? "",
            Email = request.Email,
            UserSession = session,
            StripeCustomerId =  customerId,
            StripeAccountId = accountId,
        };
        // Todo: add to user database
    }
}