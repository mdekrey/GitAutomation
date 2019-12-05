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
            var name = parameters.Name;
            var branchDetails = parameters.ReserveFullState.BranchDetails;
            var upstreamReserves = parameters.ReserveFullState.UpstreamReserves;
            var reserve = parameters.ReserveFullState.Reserve;

            await Task.Yield();
            using var repo = new Repository(parameters.WorkingPath);
            var changedBranches = branchDetails.Keys.Where(bd => branchDetails[bd] != reserve.IncludedBranches[bd].LastCommit);
            var changedReserves = upstreamReserves.Keys.Where(r => upstreamReserves[r].OutputCommit != reserve.Upstream[r].LastOutput);

            if (changedBranches.Any() || changedReserves.Any())
            {

                if (upstreamReserves.Any())
                {
                    dispatcher.Dispatch(new SetReserveOutOfDateAction { Reserve = name }, agent, $"Changes occurred to '{name}'; need to update");
                }
                else
                {
                    dispatcher.Dispatch(new StabilizeNoUpstreamAction
                    { 
                        Reserve = name, 
                        BranchCommits = changedBranches.ToDictionary(b => b, b => branchDetails[b])
                    }, agent, $"Changes occurred to '{name}'; need to update");
                }
            }
        }
    }
}
