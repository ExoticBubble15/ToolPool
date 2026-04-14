using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using Supabase;
using System.Text;
using ToolPool.Client.Services;
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

builder.Services.Configure<ToolPool.Models.SupabaseOptions>(builder.Configuration.GetSection("Supabase"));
builder.Services.Configure<SendbirdOptions>(builder.Configuration.GetSection("Sendbird"));
builder.Services.AddHttpClient();
builder.Services.AddScoped<SupabaseDemoService>();
builder.Services.AddScoped<SendbirdService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ToolPool.Client.Services.CartService>(); 
builder.Services.AddScoped<ToolPool.Client.Services.DemoItemService>();
builder.Services.AddScoped<StripePaymentService>();

builder.Services.AddScoped<HttpClient>(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(googleo =>
{
    googleo.ClientId = builder.Configuration["Google:ClientID"];
    googleo.ClientSecret = builder.Configuration["Google:ClientSecret"];
    googleo.CallbackPath = "/signin-google";
});

// supabase client setup ** UNTESTED **
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
