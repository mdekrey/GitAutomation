using GitAutomation.DomainModels;
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
                    .Select(b => HistoryInfo(b.FriendlyName, b.Tip)).ToArray(),
                Reserves = stateMachine.State.Configuration.Structure.BranchReserves
                    .Select(kvp => (key: kvp.Key, commit: repo.Lookup<Commit>(kvp.Value.OutputCommit)))
                    .Where(kvp => kvp.commit != null)
                    .Select(kvp => HistoryInfo(kvp.key, kvp.commit))
                    .ToArray()
            });

            object? HistoryInfo(string name, Commit outputCommit)
            {
                if (outputCommit == null)
                {
                    return new
                    {
                        name,
                        Commit = BranchReserve.EmptyCommit,
                        Behind = int.MaxValue,
                        Ahead = 0
                    };
                }
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
