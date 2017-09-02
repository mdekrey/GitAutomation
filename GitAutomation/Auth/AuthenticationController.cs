using Microsoft.AspNetCore.Authentication;
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
        public IActionResult SignIn([FromServices] IOptions<Plugins.AuthenticationOptions> options)
        {
            // TODO
            return this.Challenge(new AuthenticationProperties
            {
                RedirectUri = "/"
            }, options.Value.Scheme);
        }

        [HttpGet("claims")]
        public IActionResult GetClaims()
        {
            return Ok(new {
                Claims = User.Claims.Select(claim => new { Type = claim.Type, Value = claim.Value }),
                Roles = User.Claims.Where(claim => claim.Type == Names.RoleType).Select(claim => claim.Value),
            });
        }
    }
}
