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
        public async Task<IActionResult> GetClaims()
        {
            var userResult = await HttpContext.AuthenticateAsync(Names.OAuthAuthenticationScheme);
            if (userResult.None)
            {
                return NotFound();
            }
            return Ok(userResult.Principal.Claims.Select(claim => new { Type = claim.Type, Value = claim.Value }));
        }
    }
}
