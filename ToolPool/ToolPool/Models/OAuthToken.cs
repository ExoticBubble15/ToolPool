namespace ToolPool.Models
{
    public sealed class OAuthToken
    {
        private string? AccessToken { get; set; }
        private string? RefreshToken { get; set; }
        private DateTime? Expiry { get; set; }
    }
}
