using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity.Data;
using ToolPool.Services;
using ToolPool.Models;
using ToolPool.Client.Models;

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
            var csession =  await _supabase.Auth.SignIn(request.Email, request.Password);
            // Create Stripe Customer
            var customerId = await _stripe.CreateCustomerAsync(request.Email);
            // Create Stripe Seller Account
            var accountId = await _stripe.CreateConnectedAccountAsync(request.Email);
            // create sendbird id
            var sendbirdId = await _sendbird.CreateOrGetUserAsync(response?.User?.Id ?? "", request.Username);


            // 4. Save to Supabase
            var newUser = new User
            {
                Id = Guid.TryParse(response?.User?.Id, out var parsedUserId) ? parsedUserId : Guid.Empty,
                Username = request.Username,
                Email = request.Email,
                Session = csession,
                Stripe_Customer_Id = customerId,
                Stripe_Account_Id = accountId,
                Sendbird_User_Id = sendbirdId,
                IsValid = true,
                avg_rating = 0.0,
                total_ratings = 0
            };

            var payload = new
            {
                id = response?.User?.Id,
                stripe_account_id = accountId,
                stripe_customer_id = customerId,
                sendbird_user_id = sendbirdId,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,
                email = request.Email,
                username = request.Username,
                avg_rating = 0.0,
                total_ratings = 0,
                session = csession,
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

    public async Task<LoginStatus> LoginUserAsync(ToolPool.Client.Models.LoginRequest request)
    {
        try
        {
            var response = await _supabase.Auth.SignIn(request.Email, request.Password);
            if (response?.User == null)
            {
                return new LoginStatus
                {
                    success = false,
                    failureMessage = "Failed to Login"
                };
            }
            else
            {
                var user = await _db.GetUserAsync(request.Email);
                user?.Session = response;
                await _db.UpdateUserSessionAsync(request.Email, response);
                return new LoginStatus { success = true, };
            }
        }
        catch (Exception ex) 
        {
            return new LoginStatus { 
                success = false,
                failureMessage = "Wrong Email or Password"
            };
        }
    }

    public async Task DeleteAccountAsync(Guid userId)
    {
        var user = await _db.GetProfileUserAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User record not found.");
        }

        await _db.DeleteRatingsByUserAsync(userId);
        await _db.DeleteInterestSubmissionsByUserAsync(userId);
        await _db.DeleteToolsByOwnerAsync(userId);
        await _db.DeletePublicUserAsync(userId);
        await _db.DeleteAuthUserAsync(userId);
    }
}
