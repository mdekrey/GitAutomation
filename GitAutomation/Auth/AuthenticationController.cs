using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GitAutomation.Auth
{
    [Route("api/[controller]")]
    public class AuthenticationController : Controller
    {

        [HttpGet("sign-in")]
        public IActionResult SignIn()
        {
            return this.Challenge(new AuthenticationProperties
            {
                RedirectUri = "/"
            }, Auth.Constants.AuthenticationScheme);
        }

        [HttpGet("sign-out")]
        public IActionResult SignOut()
        {
            return this.SignOut(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
