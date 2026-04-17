using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToolPool.Models;

namespace ToolPool.Services;

public class OAuthLoginService : Controller
{
    private readonly SupabaseDemoService _supabase;
    private readonly IWebHostEnvironment _env;

    public OAuthLoginService(SupabaseDemoService supabase, IWebHostEnvironment env)
    {
        _supabase = supabase;
        _env = env;
    }

    [HttpGet("/auth/google")]
    public IActionResult GoogleLogin()
    {
        var ops = new AuthenticationProperties
        {
            RedirectUri = "/"
        };

        return Challenge(ops, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }

    /// <summary>
    /// Returns the authenticated user's profile. Resolves from claims:
    /// 1. Prefer NameIdentifier (app user id) claim → look up by ID
    /// 2. Fallback to Email claim → look up by email
    /// 3. Auto-create Users row if email is present but no row exists
    /// </summary>
    [HttpGet("/api/auth/me")]
    public async Task<IActionResult> Me()
    {
        if (HttpContext.User.Identity?.IsAuthenticated != true)
            return Unauthorized(new { error = "Not authenticated" });

        AppUser? user = null;

        // Prefer app user id claim
        var idClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(idClaim) && Guid.TryParse(idClaim, out var userId))
        {
            user = await _supabase.GetUserByIdAsync(userId);
        }

        // Fallback to email claim
        if (user is null)
        {
            var emailClaim = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(emailClaim))
            {
                user = await _supabase.GetUserByEmailAsync(emailClaim);

                // Auto-create Users row if missing
                if (user is null)
                {
                    var nameClaim = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? emailClaim;
                    user = await _supabase.CreateUserAsync(Guid.NewGuid(), emailClaim, nameClaim);
                }
            }
        }

        if (user is null)
            return Unauthorized(new { error = "Could not resolve user" });

        return Ok(user);
    }

    /// <summary>
    /// Development-only: simulate login by setting a cookie for an existing Users row.
    /// Accepts { "identifier": "user-id-or-email" }.
    /// </summary>
    [HttpPost("/api/auth/dev-login")]
    public async Task<IActionResult> DevLogin([FromBody] DevLoginRequest request)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Identifier))
            return BadRequest(new { error = "identifier is required" });

        AppUser? user = null;

        if (Guid.TryParse(request.Identifier, out var id))
            user = await _supabase.GetUserByIdAsync(id);

        user ??= await _supabase.GetUserByEmailAsync(request.Identifier);

        if (user is null)
            return NotFound(new { error = "User not found" });

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Name, user.Username ?? "")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return Ok(user);
    }

    [HttpPost("/api/auth/logout")]
    public async Task<IActionResult> ApiLogout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { success = true });
    }

    public class DevLoginRequest
    {
        public string Identifier { get; set; } = "";
    }
}
