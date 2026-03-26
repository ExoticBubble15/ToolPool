using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Stripe;
using ToolPool.Components;
using ToolPool.Models;
using ToolPool.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// ADDITIONS
builder.Services.AddControllers(); //api
builder.Configuration.AddUserSecrets<Program>(); //user secrets

builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection("Supabase"));
builder.Services.AddHttpClient();
builder.Services.AddScoped<SupabaseDemoService>();


// services for stripe
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ToolPool.Client.Services.CartService>(); 
builder.Services.AddScoped<ToolPool.Client.Services.DemoItemService>();
builder.Services.AddScoped<StripePaymentService>();

// start http client
builder.Services.AddScoped<HttpClient>(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

// Google OAuth
builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(googleo =>
{
    googleo.ClientId = "placeholder - clientidhere";
    googleo.ClientSecret = "placeholder - clientsecrethere";
    googleo.CallbackPath = "/signin-goole";
});

// config stripe
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ToolPool.Client._Imports).Assembly);

// ADDITIONS
app.MapControllers(); //api

app.Run();
