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

builder.Services.AddControllers();
builder.Configuration.AddUserSecrets<Program>();

builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection("Supabase"));
builder.Services.Configure<SendbirdOptions>(builder.Configuration.GetSection("Sendbird"));
builder.Services.AddHttpClient();
builder.Services.AddScoped<SupabaseDemoService>();
builder.Services.AddScoped<SendbirdService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ToolPool.Client.Services.CartService>(); 
builder.Services.AddScoped<ToolPool.Client.Services.ToolService>();
builder.Services.AddScoped<StripePaymentService>();

builder.Services.AddScoped<HttpClient>(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

// builder.Services.AddAuthentication(o =>
// {
//     o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//     o.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
// })
// .AddCookie()
// .AddGoogle(googleo =>
// {
//     googleo.ClientId = "";
//     googleo.ClientSecret = "";
//     googleo.CallbackPath = "/signin-google";
// });

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

app.MapControllers();

app.Run();
