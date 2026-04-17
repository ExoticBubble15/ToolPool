namespace ToolPool.Models
{
    public class StripeRentalRequest
    {
        public Guid ToolId { get; set; }
        public string ToolName { get; set; } = "";
        public decimal PricePerDay { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public Guid UserId { get; set; }
        public string? Message { get; set; }
    }
}
