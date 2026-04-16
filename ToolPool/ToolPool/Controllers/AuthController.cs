using System.Diagnostics.Tracing;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OneOf.Types;
using ToolPool.Models;
using ToolPool.Services;

namespace ToolPool.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : Controller
    {
        private readonly Supabase.Client supabase;
        private readonly UserService _userService;
        private readonly SupabaseDemoService _supabaseService;
        private Dictionary<String, User> _users;

        public AuthController(Supabase.Client _supabase, UserService userService, SupabaseDemoService supabaseService, Dictionary<string, User> users)
        {
            supabase = _supabase;
            _userService = userService;
            _supabaseService = supabaseService;
            _users = users;
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
            // remove user from user cache
            string email = User.Identity?.Name ?? "";
            if (!string.IsNullOrEmpty(email)) _users.Remove(email);

            // clear auth cookies
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            // supabase auth handled clientside...
        }

        [HttpGet("status")]
        public async Task<IActionResult> AuthStatus()
        {
            return Ok(new
            {
                Email = User.Identity?.Name
            });
        }

        [HttpPost("signin")]
        public async Task<IActionResult> TrySignIn([FromBody] ToolPool.Client.Models.LoginRequest request)
        {
            var loginStatus = await _userService.LoginUserAsync(request);
            if (!loginStatus.success) return Ok(loginStatus);

            // if login success, add the user to the cache
            var user = await _supabaseService.GetUserAsync(request.Email);
            if (user == null) return Ok(new Client.Models.LoginStatus { success = false });
            _users[request.Email] = user;

            // issue auth cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, request.Email)
            };
            var identity = new ClaimsIdentity(claims, "cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });
            
            return Ok(loginStatus);
        }
    }

    //[HttpGet("getUserIDs")]
    //public async Task<IActionResult> GetUserIDs()
    //    {
    //        var curUser = 
    //    }
}
