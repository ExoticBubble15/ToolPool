using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using Stripe;
using Stripe.Climate;
using ToolPool.Client.Pages;
using ToolPool.Client.Services;
using ToolPool.Components;
using ToolPool.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// ADDITIONS
builder.Services.AddControllers(); //api
builder.Configuration.AddUserSecrets<Program>(); //user secrets

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartService>();
builder.Services.AddSingleton<DemoItemService>();
builder.Services.AddScoped<StripePaymentService>();

builder.Services.AddScoped<HttpClient>(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
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

// ADDITIONS
app.MapControllers(); //api

app.Run();

