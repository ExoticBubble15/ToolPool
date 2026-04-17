using System.Text.Json.Serialization;

namespace ToolPool.Models
{
    public class InterestRequest
    {
        [JsonPropertyName("tool_id")]
        public Guid ToolId { get; set; }

        [JsonPropertyName("tool_name")]
        public string ToolName { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("start_date")]
        public string? StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public string? EndDate { get; set; }

    }
}
