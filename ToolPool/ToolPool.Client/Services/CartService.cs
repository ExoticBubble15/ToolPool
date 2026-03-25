/**
 * Service for managing Cart in Stripe Demo
 **/

using ToolPool.Client.Models;

namespace ToolPool.Client.Services;

public class CartService
{
    // Internal list of items currently in the cart
    private List<CartItem> _items = new();

    // Public access to the cart items
    public List<CartItem> Items { get { return _items; } }

    // Sum of all item prices in the cart
    public decimal Total => _items.Sum(i => i.Price);

    // Number of items in the cart
    public int Count => _items.Count;

    // Returns true if a DemoItem is already in the cart (prevents duplicates)
    public bool IsInCart(Guid demoItemId) => _items.Any(i => i.DemoItemId == demoItemId);

    // Adds a DemoItem to the cart as a CartItem
    // Ignores the add if the item is already in the cart
    public void Add(DemoItem item)
    {
        if (IsInCart(item.Id))
            return;

        _items.Add(new CartItem
        {
            DemoItemId = item.Id,
            Name = item.Name,
            Price = item.Price
        });
    }

    // Removes a CartItem by its own Id
    public void Remove(Guid cartItemId)
    {
        _items.RemoveAll(i => i.Id == cartItemId);
    }

    // Empties the cart
    public void Clear() => _items.Clear();

    // Event to render ui on changes to cart
    public event Action? OnChange;

    // Activates OnChange and tells subscribers
    public void NotifyChanged() => OnChange?.Invoke();
}