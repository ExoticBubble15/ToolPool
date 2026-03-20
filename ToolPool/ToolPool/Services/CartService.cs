/**
 * Service for managingg Cart in Stripe Demo
 **/

using ToolPool.Models;

namespace ToolPool.Services
{
    public class CartService
    {
        private List<CartItem> _items = new();

        public List<CartItem> Items { get { return _items; } }

        public decimal Total => _items.Sum(i => i.Price);

        public int Count => _items.Count;

        public bool IsInCart(Guid demoItemId) => _items.Any(i => i.DemoItemId == demoItemId);

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

        public void Remove(Guid cartItemId)
        {
           _items.RemoveAll(i => i.Id == cartItemId);
        }

        public void Clear() => _items.Clear();

        public event Action? OnChange;
        public void NotifyChanged() => OnChange?.Invoke();

    }
}
