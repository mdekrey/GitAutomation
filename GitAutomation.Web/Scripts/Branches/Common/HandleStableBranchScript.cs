using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Actions;
using GitAutomation.Scripting;
using GitAutomation.Web;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Branches.Common
{
    public class HandleStableBranchScript : IScript<ReserveScriptParameters>
    {
        private readonly IDispatcher dispatcher;

        public HandleStableBranchScript(IDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public async Task Run(ReserveScriptParameters parameters, ILogger logger, IAgentSpecification agent)
        {
            await Task.Yield();
            var changedBranches = parameters.BranchDetails.Keys.Where(bd => parameters.BranchDetails[bd] != parameters.Reserve.IncludedBranches[bd].LastCommit).ToArray();
            var changedReserves = parameters.UpstreamReserves.Keys.Where(r => parameters.UpstreamReserves[r].OutputCommit != parameters.Reserve.Upstream[r].LastOutput).ToArray();

            if (changedBranches.Any() || changedReserves.Any())
            {
                logger.LogInformation($"Reserve {parameters.Name} changes: branches ({string.Join(',', changedBranches)}), reserves ({string.Join(',', changedReserves)})");
                if (parameters.UpstreamReserves.Any())
                {
                    dispatcher.Dispatch(new SetReserveOutOfDateAction { Reserve = parameters.Name }, agent, $"Changes occurred to '{parameters.Name}'; need to update");
                }
                else
                {
                    dispatcher.Dispatch(new StabilizeNoUpstreamAction
                    { 
                        Reserve = parameters.Name, 
                        BranchCommits = changedBranches.ToDictionary(b => b, b => parameters.BranchDetails[b])
                    }, agent, $"Changes occurred to '{parameters.Name}'; need to update");
                }
            }
        }
    }
}
