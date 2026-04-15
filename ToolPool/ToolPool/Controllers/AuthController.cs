using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ToolPool.Models;

namespace ToolPool.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : Controller
    {
        private readonly Supabase.Client supabase;
        private readonly UserService _userService;

        public AuthController(Supabase.Client _supabase, UserService userService)
        {
            supabase = _supabase;
            _userService = userService;
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
            var session = supabase.Auth.CurrentSession;

            return Ok(session != null);
        }

        [HttpGet("signup")]
        public async Task<IActionResult> RegisterUser(string email, string password)
        {
            RegisterRequest req = new RegisterRequest { Email = email, Password = password };
            User result = await _userService.RegisterUserAsync(req);
            if (result.IsValid)
            {
                return Ok(new User
                {
                    IsValid = true
                });
            }
            else
            {
                return Ok(new User
                {
                    IsValid = false,
                    ErrorMessage = result.ErrorMessage
                });
            }
        }
    }
}
