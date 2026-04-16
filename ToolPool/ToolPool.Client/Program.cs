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

// clientside supabase client
// supabase client setup
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var options = new Supabase.SupabaseOptions
    {
        AutoConnectRealtime = true
    };

    var client = new Supabase.Client(builder.Configuration["Supabase:Url"], builder.Configuration["Supabase:AnonKey"], options);
    client.InitializeAsync();

    return client;
});

builder.Services.AddBlazorGoogleMaps("");

builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<DemoItemService>();
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
