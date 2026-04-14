using GoogleMapsComponents;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ToolPool.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

//get localhost
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

//blazor google maps
var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
string googleMapsKey = await httpClient.GetStringAsync("/api/getSecret/Google:Maps");
builder.Services.AddBlazorGoogleMaps(googleMapsKey);

//blazor bootstrap
builder.Services.AddBlazorBootstrap();

builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<DemoItemService>();
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
