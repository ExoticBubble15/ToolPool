using System.Text.Json.Serialization;

namespace ToolPool.Models
{
    public class AddressPair
    {
        public string Address { get; set; } = string.Empty;
        public string PlaceId { get; set; } = string.Empty;
    }

    public class Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class ToolCategory
    {
        public string Category {  get; set; } = string.Empty;
    }

    public class ToolNeighborhood
    {
        public string Neighborhood { get; set; } = string.Empty;
    }

    public class NeighborhoodTuple
    {
        public string neighborhood { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
    }
    public class Tool
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("owner_id")]
        public Guid? OwnerId { get; set; }

        [JsonPropertyName("owner_name")]
        public string OwnerName { get; set; } = string.Empty;

        public string Neighborhood { get; set; } = string.Empty;

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }
    }

    public class CartItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DemoItemId { get; set; }
        public decimal Price { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class InterestSubmission
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonPropertyName("tool_id")]
        public Guid ToolId { get; set; }

        [JsonPropertyName("tool_name")]
        public string ToolName { get; set; } = string.Empty;

        [JsonPropertyName("renter_id")]
        public string RenterId { get; set; } = string.Empty;

        [JsonPropertyName("owner_id")]
        public Guid? OwnerId { get; set; }

        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("start_date")]
        public string? StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public string? EndDate { get; set; }

        [JsonPropertyName("channel_url")]
        public string? ChannelUrl { get; set; }

        public string Status { get; set; } = "pending";

        [JsonPropertyName("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }
    }
}
