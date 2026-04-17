using System.Text.Json.Serialization;

namespace ToolPool.Client.Models;

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
}

public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DemoItemId { get; set; }
    public decimal Price { get; set; }
    public string Name { get; set; } = string.Empty;
}

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

public class StripeResponse
{
    public string Url { get; set; } = "";
}

public class AppUser
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string Username { get; set; } = "";

    [JsonPropertyName("sendbird_user_id")]
    public string? SendbirdUserId { get; set; }
}

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

public class InterestResponse
{
    public bool Success { get; set; }

    [JsonPropertyName("channel_url")]
    public string? ChannelUrl { get; set; }

    [JsonPropertyName("interest_id")]
    public Guid? InterestId { get; set; }
}

public class MyInterestItem
{
    public Guid Id { get; set; }

    [JsonPropertyName("tool_name")]
    public string ToolName { get; set; } = "";

    [JsonPropertyName("channel_url")]
    public string? ChannelUrl { get; set; }

    public string Status { get; set; } = "pending";

    [JsonPropertyName("counterpart_name")]
    public string CounterpartName { get; set; } = "";

    public string Role { get; set; } = "";

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
}

public class ChatPaymentContext
{
    [JsonPropertyName("can_pay")]
    public bool CanPay { get; set; }

    [JsonPropertyName("tool_name")]
    public string ToolName { get; set; } = "";

    [JsonPropertyName("price_per_day")]
    public decimal PricePerDay { get; set; }

    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }

    [JsonPropertyName("total_amount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
