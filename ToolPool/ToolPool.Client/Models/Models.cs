/**
 * Models for demo stripe app
 */
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

// item in cart
public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DemoItemId { get; set; }
    public decimal Price { get; set; }
    public string Name { get; set; } = string.Empty;
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