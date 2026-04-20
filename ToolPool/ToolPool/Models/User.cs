using Supabase.Gotrue;
namespace ToolPool.Models

{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public Session? Session { get; set; }
        public string? Stripe_Customer_Id { get; set; }
        public string? Stripe_Account_Id { get; set; }
        public string? Sendbird_User_Id { get; set; }
        public double? avg_rating { get; set; }
        public int? total_ratings {  get; set; }
        // For error handling
        public bool IsValid { get; set; } = false;
        public string? ErrorMessage { get; set; }
    }
}
