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
        
        [JsonPropertyName("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }

        [JsonPropertyName("owner_id")]
        public Guid? OwnerId { get; set; }

        [JsonPropertyName("owner_name")]
        public string OwnerName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public double AddressLat { get; set; }

        public double AddressLng { get; set; }

        public string Neighborhood { get; set; } = string.Empty;

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("owner_avg_rating")]
        public double? OwnerAvgRating { get; set; }

        [JsonPropertyName("owner_total_ratings")]
        public int? OwnerTotalRatings { get; set; }
    }

    public class OwnerRating
    {
        [JsonPropertyName("avg_rating")]
        public double? AvgRating { get; set; }

        [JsonPropertyName("total_ratings")]
        public int? TotalRatings { get; set; }
    }

    public class CartItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DemoItemId { get; set; }
        public decimal Price { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AppUser
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = "";
        public string Username { get; set; } = "";

        [JsonPropertyName("sendbird_user_id")]
        public string? SendbirdUserId { get; set; }

        [JsonPropertyName("stripe_account_id")]
        public string? StripeAccountId { get; set;  }
        [JsonPropertyName("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }
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

    public class ToolAddressLookup
    {
        public Guid Id { get; set; }

        [JsonPropertyName("owner_id")]
        public Guid? OwnerId { get; set; }

        public string Name { get; set; } = string.Empty;

        public double AddressLat { get; set; }

        public double AddressLng { get; set; }

        public string Neighborhood { get; set; } = string.Empty;
    }

    public class PickupAddressResponse
    {
        [JsonPropertyName("interest_id")]
        public Guid InterestId { get; set; }

        [JsonPropertyName("tool_name")]
        public string ToolName { get; set; } = string.Empty;

        public string Status { get; set; } = "pending";

        [JsonPropertyName("can_view")]
        public bool CanView { get; set; }

        [JsonPropertyName("can_reveal")]
        public bool CanReveal { get; set; }

        [JsonPropertyName("is_revealed")]
        public bool IsRevealed { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("can_start_handoff")]
        public bool CanStartHandoff { get; set; }

        [JsonPropertyName("can_confirm_pickup")]
        public bool CanConfirmPickup { get; set; }

        [JsonPropertyName("can_request_return")]
        public bool CanRequestReturn { get; set; }

        [JsonPropertyName("can_confirm_return")]
        public bool CanConfirmReturn { get; set; }

        [JsonPropertyName("can_rate_owner")]
        public bool CanRateOwner { get; set; }

        [JsonPropertyName("current_owner_rating")]
        public int? CurrentOwnerRating { get; set; }
    }

    public class Rating
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonPropertyName("interest_id")]
        public Guid InterestId { get; set; }

        [JsonPropertyName("rater_id")]
        public Guid RaterId { get; set; }

        [JsonPropertyName("rated_user_id")]
        public Guid RatedUserId { get; set; }

        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset? CreatedAt { get; set; }
    }
}
