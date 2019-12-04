using GitAutomation.Scripting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Web.Controllers
{
    [Route("api/[controller]")]
    public class HealthController : Controller
    {
        [HttpGet("configuration/last-load")]
        public IActionResult GetConfigurationLastLoad([FromServices] RepositoryConfigurationService service)
        {
            return ToResult(service.LastLoadResult);
        }

        [HttpGet("configuration/last-push")]
        public IActionResult GetConfigurationLastPush([FromServices] RepositoryConfigurationService service)
        {
            return ToResult(service.LastPushResult);
        }

        [HttpGet("target/last-load")]
        public IActionResult GetTargetLastLoad([FromServices] TargetRepositoryService service)
        {
            return ToResult(service.LastLoadFromDiskResult);
        }

        [HttpGet("target/last-fetch")]
        public IActionResult GetTargetLastFetch([FromServices] TargetRepositoryService service)
        {
            return ToResult(service.LastFetchResult);
        }

        [HttpGet("target/last-for/{*reserve}")]
        public IActionResult GetTargetLastFetch([FromServices] ReserveAutomationService service, [FromRoute] string reserve)
        {
            return ToResult(service.LastScriptForReserve(reserve));
        }

        private IActionResult ToResult(ScriptProgress streams)
        {
            if (streams == null)
            {
                return NotFound();
            }

            return streams.Completion.IsCompleted
                ? ToResult(streams.Completion.Result)
                : Ok(null); // TODO - input? output-in-progress?
        }

        private IActionResult ToResult(ScriptResult result)
        {
            return Ok(new
            {
                // TODO - input? output?
                result.Exception
            });
        }
    }
}
