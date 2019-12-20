using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Actions;
using GitAutomation.Scripts.Branches.Common.Steps;
using GitAutomation.Web;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Branches.Common
{
    public interface IFlowStrategy
    {
        MultiMergeStatus Merge(Repository repo, MultiMergeStatus previous, Commit commit, string reserveName, Identity identity);
        Task DispatchManual(Repository repo, ReserveScriptParameters parameters, ILogger logger, IAgentSpecification agent, string outputBranchName, IEnumerable<string> upstreamNeeded);
    }

    public class AutomaticFlowStrategy : IFlowStrategy
    {
        private readonly ManualFlowStrategy fallback;
        private readonly IDispatcher dispatcher;
        private readonly IIntegrationReserveUtilities integrationReserveUtilities;

        public AutomaticFlowStrategy(ManualFlowStrategy fallback, IDispatcher dispatcher, IIntegrationReserveUtilities integrationReserveUtilities)
        {
            this.fallback = fallback;
            this.dispatcher = dispatcher;
            this.integrationReserveUtilities = integrationReserveUtilities;
        }

        public async Task DispatchManual(Repository repo, ReserveScriptParameters parameters, ILogger logger, IAgentSpecification agent, string outputBranchName, IEnumerable<string> upstreamNeeded)
        {
            if (await ConflictsExistUpstream(repo, parameters, logger, agent))
            {
                HandleUpstreamConflicts(agent, parameters);
            }
            else
            {
                await fallback.DispatchManual(repo, parameters, logger, agent, outputBranchName, upstreamNeeded);
            }
        }

        public MultiMergeStatus Merge(Repository repo, MultiMergeStatus previous, Commit commit, string reserveName, Identity identity) =>
            repo.CleanMerge(commit, identity, message: $"Auto-merge {reserveName}") switch
            {
                MergeStatus.Conflicts => previous.AddManualUpstream(reserveName),
                MergeStatus.UpToDate => previous,
                MergeStatus.FastForward => previous.HadAutomaticUpdate(),
                MergeStatus.NonFastForward => previous.HadAutomaticUpdate(),
                _ => throw new NotImplementedException()
            };

        private void HandleUpstreamConflicts(IAgentSpecification agent, ReserveScriptParameters parameters)
        {
            var branchCommits = parameters.GetBranchExpectedOutputCommits();
            var reserveOutputCommits = parameters.GetReserveExpectedOutputCommits();

            // TODO - ManualInterventionNeededAction isn't really the best here, but it works
            dispatcher.Dispatch(new ManualInterventionNeededAction
            {
                BranchCommits = branchCommits,
                ReserveOutputCommits = reserveOutputCommits,
                Reserve = parameters.Name,
                State = "OutOfDate",
                NewBranches = new ManualInterventionNeededAction.ManualInterventionBranch[0],
            }, agent, $"Conflicts detected upstream from '{parameters.Name}'");
        }

        private async Task<bool> ConflictsExistUpstream(Repository repo, ReserveScriptParameters parameters, ILogger logger, IAgentSpecification agent)
        {
            await Task.Yield();
            var conflictingUpstream = new List<(string, string)>();
            var reserveNames = parameters.UpstreamReserves.Keys.OrderBy(k => k).ToArray();
            // TODO - see if any are already upstream of others. If so, remove them. (Filter? Issue removals?)
            for (var i = 0; i < reserveNames.Length - 1; i++)
            {
                for (var j = i + 1; j < reserveNames.Length; j++)
                {
                    if (!repo.ObjectDatabase.CanMergeWithoutConflict(repo.Lookup<Commit>(parameters.UpstreamReserves[reserveNames[i]].OutputCommit), repo.Lookup<Commit>(parameters.UpstreamReserves[reserveNames[j]].OutputCommit)))
                    {
                        logger.LogInformation($"Conflict found between reserves '{reserveNames[i]}' and '{reserveNames[j]}'");
                        conflictingUpstream.Add((reserveNames[i], reserveNames[j]));
                    }
                }
            }

            foreach (var action in integrationReserveUtilities.AddUpstreamConflicts(parameters.Name, conflictingUpstream, parameters.UpstreamReserves))
            {
                dispatcher.Dispatch(action, agent, $"Upstream conflicts detected in {parameters.Name}");
            }
            return conflictingUpstream.Any();
        }

    }

    public class ManualFlowStrategy : IFlowStrategy
    {
        private readonly IDispatcher dispatcher;
        private readonly TemporaryIntegrationBranchLifecycle integrationBranchLifecycle;

        public ManualFlowStrategy(IDispatcher dispatcher, TemporaryIntegrationBranchLifecycle integrationBranchLifecycle)
        {
            this.dispatcher = dispatcher;
            this.integrationBranchLifecycle = integrationBranchLifecycle;
        }

        public Task DispatchManual(Repository repo, ReserveScriptParameters parameters, ILogger logger, IAgentSpecification agent, string outputBranchName, IEnumerable<string> upstreamNeeded)
        {
            HandleManualIntegration(agent, parameters, repo, outputBranchName, upstreamNeeded);

            return Task.CompletedTask;
        }

        public MultiMergeStatus Merge(Repository repo, MultiMergeStatus previous, Commit commit, string reserveName, Identity identity) =>
            previous.AddManualUpstream(reserveName);

        private void HandleManualIntegration(IAgentSpecification agent, ReserveScriptParameters parameters, Repository repo, string outputBranchName, IEnumerable<string> upstreamNeeded)
        {
            var branchCommits = parameters.GetBranchExpectedOutputCommits();
            var reserveOutputCommits = parameters.GetReserveExpectedOutputCommits();

            var newBranches = integrationBranchLifecycle.Create(parameters, repo, upstreamNeeded);
            var message = $"Need manual merging to '{outputBranchName}' for '{parameters.Name}'";

            foreach (var branch in newBranches)
            {
                dispatcher.Dispatch(new RequestManualPullAction
                {
                    TargetBranch = outputBranchName,
                    SourceBranch = branch.Name
                }, agent, message);
            }

            dispatcher.Dispatch(new ManualInterventionNeededAction
            {
                BranchCommits = branchCommits,
                ReserveOutputCommits = reserveOutputCommits,
                Reserve = parameters.Name,
                State = "NeedsUpdate",
                NewBranches = newBranches,
            }, agent, message);
        }

    }
}
