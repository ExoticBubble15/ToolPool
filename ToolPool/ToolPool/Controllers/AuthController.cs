using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace ToolPool.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : Controller
    {
        private readonly Supabase.Client supabase;

        public AuthController(Supabase.Client _supabase)
        {
            supabase = _supabase;
        }

        [HttpGet("google")]
        public IActionResult GoogleLogin()
        {
            var ops = new AuthenticationProperties
            {
                RedirectUri = "/"
            };

            return Challenge(ops, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            // clear auth cookies
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect("/");
        }

        [HttpGet("status")]
        public async Task<IActionResult> AuthStatus()
        {
            var user = supabase.Auth.CurrentUser;

            return Ok(user != null);
        }
    }
}
