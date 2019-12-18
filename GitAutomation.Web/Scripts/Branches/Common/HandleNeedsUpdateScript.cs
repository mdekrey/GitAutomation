using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Actions;
using GitAutomation.Scripting;
using GitAutomation.Web;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Branches.Common
{
    public class HandleNeedsUpdateScript : IScript<ReserveScriptParameters>
    {
        private readonly IDispatcher dispatcher;
        private readonly TargetRepositoryOptions options;
        private readonly AutomationOptions automationOptions;

        public HandleNeedsUpdateScript(IDispatcher dispatcher, IOptions<TargetRepositoryOptions> options, IOptions<AutomationOptions> automationOptions)
        {
            this.dispatcher = dispatcher;
            this.options = options.Value;
            this.automationOptions = automationOptions.Value;
        }

        public async Task Run(ReserveScriptParameters parameters, ILogger logger, IAgentSpecification agent)
        {
            await Task.Yield();
            var outputBranches = parameters.Reserve.GetBranchesByRole("Output");
            if (outputBranches.Length != 1)
            {
                logger.LogError($"Reserve {parameters.Name} did not have exactly one output branch.", outputBranches);
                return;
            }
            var outputBranch = outputBranches.Single();

            using var repo = GitRepositoryUtilities.CloneAsLocal(options.CheckoutPath, parameters.WorkingPath, automationOptions);
            RemoveIntegratedBranches(parameters, agent, outputBranch, repo, out var updatedBranches, out var allRemoved);
            var reserveOutputCommits = parameters.UpstreamReserves.Where(r => r.Value.OutputCommit != parameters.Reserve.Upstream[r.Key].LastOutput)
                .ToDictionary(r => r.Key, r => parameters.UpstreamReserves[r.Key].OutputCommit);
            if (!allRemoved)
            {
                dispatcher.Dispatch(new ManualInterventionNeededAction
                {
                    Reserve = parameters.Name,
                    State = parameters.Reserve.Status,
                    BranchCommits = updatedBranches,
                    ReserveOutputCommits = reserveOutputCommits,
                    NewBranches = Array.Empty<ManualInterventionNeededAction.ManualInterventionBranch>(),
                }, agent, $"Reserve {parameters.Name} still needs updates");
            }
            else
            {
                dispatcher.Dispatch(new StabilizeReserveAction { Reserve = parameters.Name }, agent, $"Reserve {parameters.Name} has all pending updates");
            }
        }

        private void RemoveIntegratedBranches(ReserveScriptParameters parameters, IAgentSpecification agent, string outputBranch, Repository repo, out Dictionary<string, string> updatedBranches, out bool allRemoved)
        {
            var integrationBranches = parameters.Reserve.GetBranchesByRole("Integration");
            // TODO - if an integration branch's source is removed from upstream, remove the branch, too
            updatedBranches = new Dictionary<string, string>();
            allRemoved = true;
            var outputCommit = repo.Lookup<Commit>(outputBranch);
            foreach (var branch in integrationBranches)
            {
                var integrationCommit = repo.Lookup<Commit>(branch);
                var history = repo.ObjectDatabase.CalculateHistoryDivergence(outputCommit, integrationCommit);
                if (history.BehindBy == 0)
                {
                    dispatcher.Dispatch(new DeleteBranchAction { TargetBranch = branch }, agent, $"Branch {branch} has been merged within reserve {parameters.Name}");
                }
                else
                {
                    allRemoved = false;
                    if (integrationCommit.Sha != parameters.Reserve.IncludedBranches[branch].LastCommit)
                    {
                        updatedBranches[branch] = integrationCommit.Sha;
                    }
                }
            }
        }
    }
}
