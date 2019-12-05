using GitAutomation.State;
using GitAutomation.Web.State;
using LibGit2Sharp;
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

        [HttpGet("revision-diffs/{*commitish}")]
        public IActionResult GetTargetLastFetch(
            [FromRoute] string commitish,
            [FromServices] IOptions<TargetRepositoryOptions> options,
            [FromServices] IStateMachine<AppState> stateMachine)
        {
            using var repo = new LibGit2Sharp.Repository(options.Value.CheckoutPath);

            var commit = (Commit)repo.Lookup(commitish, LibGit2Sharp.ObjectType.Commit);

            return Ok(new
            {
                Branches = repo.Branches.Where(b => b.CanonicalName.StartsWith("refs/heads/"))
                    .Select(b => HistoryInfo(b.FriendlyName, b.Tip)),
                Reserves = stateMachine.State.Configuration.Structure.BranchReserves
                    .Select(kvp => HistoryInfo(kvp.Key, (Commit)repo.Lookup(kvp.Value.OutputCommit, ObjectType.Commit)))
            });

            object HistoryInfo(string name, Commit outputCommit)
            {
                var historyDivergence = repo.ObjectDatabase.CalculateHistoryDivergence(commit, outputCommit);
                return new
                {
                    name,
                    Commit = outputCommit.Id,
                    Behind = historyDivergence.BehindBy,
                    Ahead = historyDivergence.AheadBy
                };
            }
        }

        
    }
}
