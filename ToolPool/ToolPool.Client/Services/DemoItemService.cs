using Stripe;
using ToolPool.Client.Models;

namespace ToolPool.Client.Services;

public class DemoItemService
{
    public List<DemoItem> GetItems() => new()
{
    new() { Name = "Bike", Description = "It works", Price = 25.00m },
    new() { Name = "DSLR Camera", Description = "Canon EOS R5 with 3 lenses", Price = 80.00m},
    new() { Name = "Camping Tent", Description = " waterproof tent", Price = 20.00m},
    new() { Name = "Kayak", Description = "Single seat, paddle included", Price = 45.00m},
    new() { Name = "Drone", Description = "4k video", Price = 60.00m},
    new() { Name = "Shovel", Description = "Titanium steel", Price = 30.00m},
};
}
