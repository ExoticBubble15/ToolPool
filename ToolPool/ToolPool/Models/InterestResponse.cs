using System.Text.Json.Serialization;

namespace ToolPool.Models
{
    public class InterestResponse
    {
        public bool Success { get; set; }

        [JsonPropertyName("channel_url")]
        public string? ChannelUrl { get; set; }

        [JsonPropertyName("interest_id")]
        public Guid? InterestId { get; set; }
    }
}
