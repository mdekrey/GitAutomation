using GitAutomation.State;
using GitAutomation.Web.State;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Web.Controllers
{
    [Route("api/[controller]")]
    public class GitController : Controller
    {

        //[HttpGet("revision-diffs/{*commitish}")]
        //public async Task<IActionResult> GetTargetLastFetch([FromRoute] string commitish, [FromServices] IOptions<TargetRepositoryOptions> options, [FromServices] IStateMachine<AppState> stateMachine)
        //{
            
        //    var streams = await PowerShell.Create()
        //        .AddUnrestrictedCommand("./Scripts/Repository/gitRevisionComparison.ps1")
        //        .BindParametersToPowerShell(new { options.Value.CheckoutPath, revision = commitish, stateMachine.State.Configuration.Structure.BranchReserves })
        //        .InvokeAllStreams<JToken, string>(JToken.Parse);
        //    return Ok(streams.Success[0]);
        //}

    }
}
