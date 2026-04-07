using GoogleMapsComponents;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ToolPool.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

// comment this out if don't have a google maps api key in secrets and hardcode key in AddBlazorGoogleMaps() method below
//string googleMapsKey = await httpClient.GetStringAsync("/api/getSecret/GoogleMapsApiKey");

builder.Services.AddBlazorGoogleMaps("");

builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<DemoItemService>();

await builder.Build().RunAsync();
