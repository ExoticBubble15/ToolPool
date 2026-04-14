namespace ToolPool.Models
{
    public class User
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = "";
        public string? StripeCustomerId { get; set; }
        public string? StripeAccountId { get; set; }

    }
}
