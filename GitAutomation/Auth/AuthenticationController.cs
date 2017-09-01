using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Auth
{
    [Route("api")]
    public class AuthenticationController : Controller
    {

        [HttpGet("sign-in")]
        public IActionResult SignIn()
        {
            // TODO
            return this.Challenge(new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                RedirectUri = "/"
            }, Names.OAuthAuthenticationScheme);
        }
    }
}
