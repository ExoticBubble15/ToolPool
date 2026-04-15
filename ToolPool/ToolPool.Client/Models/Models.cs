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
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}