namespace ToolPool.Client.Models;

public class DemoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DemoItemId { get; set; }
    public decimal Price { get; set; }
    public string Name { get; set; } = string.Empty;
}

