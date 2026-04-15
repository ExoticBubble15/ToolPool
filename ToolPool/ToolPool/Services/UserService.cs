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

    public async Task<User> RegisterUserAsync(ToolPool.Models.RegisterRequest request)
    {
        // Create user in Supabase
        // user alr exists try catch
        try
        {
            // TODO: error handling for all these below
            var response = await _supabase.Auth.SignUp(request.Email, request.Password);
            //var session =  await _supabase.Auth.SignIn(request.Email, request.Password);
            // Create Stripe Customer
            var customerId = await _stripe.CreateCustomerAsync(request.Email);
            // Create Stripe Seller Account
            var accountId = await _stripe.CreateConnectedAccountAsync(request.Email);
            // create sendbird id
            var sendbirdId = await _sendbird.CreateOrGetUserAsync(response?.User?.Id ?? "", request.Email);


            // 4. Save to Supabase
            var newUser = new User
            {
                Username = response?.User?.Id ?? "",
                Email = request.Email,
                //UserSession = session,
                StripeCustomerId = customerId,
                StripeAccountId = accountId,
                SendBirdId = sendbirdId,
                IsValid = true
            };
            // Todo: add to user database

            var payload = new
            {
                id = response?.User?.Id,
                stripe_account_id = accountId,
                stripe_customer_id = customerId,
                sendbird_user_id = sendbirdId,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
                email = request.Email
            };

            await _db.InsertUserAsync(payload);

            return newUser;
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            Console.WriteLine("=== GOTRUE ERROR ===");
            Console.WriteLine("Message:");
            Console.WriteLine(ex.Message);

            Console.WriteLine("Content:");
            Console.WriteLine(ex.Content);

            Console.WriteLine("Full:");
            Console.WriteLine(ex.ToString());

            throw;
        }
    }
}