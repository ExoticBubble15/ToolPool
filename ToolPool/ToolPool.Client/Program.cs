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
string googleMapsKey = await httpClient.GetStringAsync("/api/getSecret/GoogleMapsApiKey");
builder.Services.AddBlazorGoogleMaps(googleMapsKey);

//blazor bootstrap
builder.Services.AddBlazorBootstrap();

builder.Services.AddSingleton<CartService>();
builder.Services.AddScoped<DemoItemService>();

await builder.Build().RunAsync();
