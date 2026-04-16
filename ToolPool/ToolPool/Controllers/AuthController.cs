using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using ToolPool.Services;

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

        [Authorize]
        [HttpGet("status")]
        public async Task<IActionResult> AuthStatus()
        {
            return Ok(new
            {
                Email = User.Identity?.Name
            });
        }

        [HttpPost("signup")]
        public async Task<IActionResult> RegisterUser([FromBody] ToolPool.Models.RegisterRequest request)
        {
            try
            {
                var result = await _userService.RegisterUserAsync(request);

                if (!result.IsValid)
                {
                    return BadRequest(new
                    {
                        error = result.ErrorMessage
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message
                });
            }
        }

        [HttpPost("signin")]
        public async Task<IActionResult> TrySignIn([FromBody] ToolPool.Client.Models.LoginRequest request)
        {
            var loginStatus = await _userService.LoginUserAsync(request);
            if (!loginStatus.success) return Ok(loginStatus);
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
}
