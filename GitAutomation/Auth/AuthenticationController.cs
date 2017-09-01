using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Auth
{
    [Route("api/[controller]")]
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

        [HttpGet("claims")]
        public IActionResult GetClaims()
        {
            return Ok(User.Claims.Select(claim => new { Type = claim.Type, Value = claim.Value }));
        }
    }
}
