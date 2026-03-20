using GoogleMapsComponents;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
string googleMapsKey = await httpClient.GetStringAsync("/api/getSecret/GoogleMapsApiKey");
builder.Services.AddBlazorGoogleMaps(googleMapsKey);

await builder.Build().RunAsync();
