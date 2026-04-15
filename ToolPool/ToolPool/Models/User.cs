using Supabase.Gotrue;
namespace ToolPool.Models

{
    public class User
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public Session? UserSession { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? StripeAccountId { get; set; }
        public string? SendBirdId { get; set; }
        // For error handling
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
