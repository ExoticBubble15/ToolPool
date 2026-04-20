namespace ToolPool.Models
{
    public class CheckAvailabilityRequest
    {
        public Guid ToolId { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
    }
}
