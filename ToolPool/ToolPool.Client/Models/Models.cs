using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace ToolPool.Client.Models;

// item in catalog
public class DemoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl {get; set; } = string.Empty;
    public decimal Price { get; set; }
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
    public Guid OwnerId { get; set; }

    [JsonPropertyName("owner_name")]
    public string OwnerName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public double AddressLat { get; set; }

    public double AddressLng { get; set; }

    public string Neighborhood { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string ImageUrl { get; set; } = string.Empty;
}

public class MarkerDetails
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; }

    public string Owner_name { get; set; } = string.Empty;
    
    public decimal Price { get; set; }

    public double AddressLat { get; set; }
    
    public double AddressLng { get; set; }
}
public class NeighborhoodTriple
{
    public string Neighborhood { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
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
    public string? UserEmail { get; set; }
    public Guid OwnerId { get; set; }
}
public class AvailabilityResponse
{
    public bool HasConflict { get; set; }
    public string? Message { get; set; }
}
public class StripeResponse
{
    public string Url { get; set; } = "";
}

public class AppUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string Username { get; set; } = "";

    [JsonPropertyName("sendbird_user_id")]
    public string? SendbirdUserId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
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
// user registration status
public class RegistrationStatus
{
    public string? username { get; set; }
    public bool? isAuthed { get; set; }
}

public class LoginStatus
{
    public bool success { get; set; }
    public string? failureMessage { get; set; }
}

public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Username { get; set; } = "";
}

public class LoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class userIDs
{
    public string? StripeCustomerId { get; set; }
    public string? StripeAccountId { get; set; }
    public string? SendBirdId { get; set; }
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

public class AuthStatus
{
    public bool authenticated { get; set; }
    public string? email { get; set; }
    public string? uid { get; set; }
}