/**
 * Service for managing Cart in Stripe Demo
 **/

using System.Text.Json;
using Microsoft.JSInterop;
using ToolPool.Client.Models;

namespace ToolPool.Client.Services;

public class CartService
{
    // we will use local storage for persistence
    private readonly IJSRuntime _js;
    private const string StorageKey = "toolpool_cart";

    private List<CartItem> _items = new();

    public CartService(IJSRuntime js) => _js = js;

    // Public access to the cart items
    public List<CartItem> Items => _items;

    // Sum of all item prices in the cart
    public decimal Total => _items.Sum(i => i.Price);

    // Number of items in the cart
    public int Count => _items.Count;

    // Returns true if a DemoItem is already in the cart (prevents duplicates)
    public bool IsInCart(Guid demoItemId) => _items.Any(i => i.DemoItemId == demoItemId);

    // Load cart from local storage
    public async Task LoadAsync()
    {
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        Console.WriteLine($"[CartService.LoadAsync] json = {json}");
        if (!string.IsNullOrEmpty(json))
            _items = JsonSerializer.Deserialize<List<CartItem>>(json) ?? new();
    }

    // Adds a DemoItem to the cart as a CartItem
    // Ignores the add if the item is already in the cart
    public async Task AddAsync(DemoItem item)
    {
        if (IsInCart(item.Id))
            return;

        _items.Add(new CartItem
        {
            DemoItemId = item.Id,
            Name = item.Name,
            Price = item.Price
        });

        await SaveAsync();
    }

    // Removes a CartItem by its own Id
    public async Task RemoveAsync(Guid cartItemId)
    {
        _items.RemoveAll(i => i.Id == cartItemId);
        await SaveAsync();
    }

    // Empties the cart
    public async Task ClearAsync()
    {
        _items.Clear();
        await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        NotifyChanged();
    }

    // Event to notify UI on changes to cart
    public event Action? OnChange;

    // Activates OnChange and tells subscribers
    public void NotifyChanged() => OnChange?.Invoke();

    // Persists current cart state to localStorage
    private async Task SaveAsync()
    {
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey,
            JsonSerializer.Serialize(_items));
        NotifyChanged();
    }
}