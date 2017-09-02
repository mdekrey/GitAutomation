using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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

        [HttpGet("claims")]
        public IActionResult GetClaims()
        {
            return Ok(new {
                Claims = User.Claims.Select(claim => new { Type = claim.Type, Value = claim.Value }),
                Roles = User.Claims.Where(claim => claim.Type == Constants.RoleType).Select(claim => claim.Value),
            });
        }
    }
}
