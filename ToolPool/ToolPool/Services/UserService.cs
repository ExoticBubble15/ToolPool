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
    private readonly SendbirdService _sendbird;

    public UserService(SupabaseDemoService db, StripePaymentService stripe, Supabase.Client supabase, SendbirdService sendbird, HttpClient http)
    {
        _db = db;
        _stripe = stripe;
        _supabase = supabase;
        _sendbird = sendbird;
        _http = http;
    }

    public async Task<User> RegisterUserAsync(RegisterRequest request)
    {
        // Create user in Supabase
        // user alr exists try catch
        try
        {
            // TODO: error handling for all these below
            var session = await _supabase.Auth.SignUp(request.Email, request.Password);
            // Create Stripe Customer
            var customerId = _stripe.CreateCustomer(request.Email);
            // Create Stripe Seller Account
            var accountId = _stripe.CreateConnectedAccount(request.Email);
            // create sendbird id
            var sendbirdId = await _sendbird.CreateOrGetUserAsync(request.Email, session?.User?.Id ?? "");


            // 4. Save to Supabase
            var newUser = new User
            {
                Username = session?.User?.Id ?? "",
                Email = request.Email,
                UserSession = session,
                StripeCustomerId = customerId,
                StripeAccountId = accountId,
                SendBirdId = sendbirdId,
                IsValid = true
            };
            // Todo: add to user database

            return newUser;
        }
        catch (Exception ex)
        {
            // kinda nasty way of passing a error message
            var userError = new User
            {
                UserSession = null,
                IsValid = false,
                ErrorMessage = ex.Message
            };
            return userError;
        }
    }
}