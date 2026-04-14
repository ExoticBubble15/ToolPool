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

// user auth status
public class AuthStatus
{
    public string? username { get; set; }
    public bool? isAuthed { get; set; }
}
