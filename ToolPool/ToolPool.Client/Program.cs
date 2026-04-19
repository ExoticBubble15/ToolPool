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

// clientside supabase client
// supabase client setup
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var options = new Supabase.SupabaseOptions
    {
        AutoConnectRealtime = true
    };

    var client = new Supabase.Client(
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InphZXBhdnFtaGR2eGFvbWpkc3p3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzM3ODQzNTUsImV4cCI6MjA4OTM2MDM1NX0.pXCCjvLHiZ_28LtJW35XQuTInqWOExUEzUQ-2hBpkYU", 
        "https://zaepavqmhdvxaomjdszw.supabase.co/"
        );
    client.InitializeAsync();

    return client;
});

builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<DemoItemService>(); 
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ToolService>();

await builder.Build().RunAsync();
