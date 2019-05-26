using GitAutomation.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;

namespace GitAutomation.Web.Controllers
{
    [Route("api/[controller]")]
    public class GitController : Controller
    {

        [HttpGet("revision-diffs/{*commitish}")]
        public async Task<IActionResult> GetTargetLastFetch([FromRoute] string commitish, [FromServices] IOptions<TargetRepositoryOptions> options)
        {
            var streams = await PowerShell.Create()
                .AddUnrestrictedCommand("./Scripts/Repository/gitRevisionComparison.ps1")
                .BindParametersToPowerShell(new { options.Value.CheckoutPath, revision = commitish })
                .InvokeAllStreams<JToken, string>(JToken.Parse);
            return Ok(streams.Success[0]);
        }

    }
}
