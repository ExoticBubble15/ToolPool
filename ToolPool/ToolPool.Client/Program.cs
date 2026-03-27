using GoogleMapsComponents;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ToolPool.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

//get google maps api key from user secrets through server api
var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
string googleMapsKey = await httpClient.GetStringAsync("/api/getSecret/GoogleMapsApiKey");
builder.Services.AddBlazorGoogleMaps(googleMapsKey);

builder.Services.AddSingleton<CartService>();
builder.Services.AddScoped<DemoItemService>();

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

await builder.Build().RunAsync();
