using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

namespace ToolPool.Services;

public class OAuthLoginService : Controller
{
    [HttpGet("/auth/google")]
    public IActionResult GoogleLogin()
    {
        var ops = new AuthenticationProperties
        {
            RedirectUri = "/"
        };

        return Challenge(ops, GoogleDefaults.AuthenticationScheme);
    }
}

