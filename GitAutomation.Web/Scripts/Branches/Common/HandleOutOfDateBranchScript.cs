using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Actions;
using GitAutomation.Scripting;
using GitAutomation.Scripts.Branches.Common.Steps;
using GitAutomation.Web;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static GitAutomation.Scripts.Branches.Common.Steps.AnyUpstreamBranchesNotStable;
using static GitAutomation.Scripts.Branches.Common.Steps.OutputBranchPreparer;

namespace GitAutomation.Scripts.Branches.Common
{
    public class HandleOutOfDateBranchScript : IScript<ReserveScriptParameters>
    {
        private readonly IDispatcher dispatcher;
        private readonly IBranchNaming branchNaming;
        private readonly IIntegrationReserveUtilities integrationReserveUtilities;
        private readonly TargetRepositoryOptions options;
        private readonly AutomationOptions automationOptions;
        private readonly IServiceProvider serviceProvider;

        public HandleOutOfDateBranchScript(IDispatcher dispatcher, IOptions<TargetRepositoryOptions> options, IOptions<AutomationOptions> automationOptions, IBranchNaming branchNaming, IIntegrationReserveUtilities integrationReserveUtilities, IServiceProvider serviceProvider)
        {
            this.dispatcher = dispatcher;
            this.branchNaming = branchNaming;
            this.integrationReserveUtilities = integrationReserveUtilities;
            this.options = options.Value;
            this.automationOptions = automationOptions.Value;
            this.serviceProvider = serviceProvider;
        }

        public async Task Run(ReserveScriptParameters parameters, ILogger logger, IAgentSpecification agent)
        {
            await Task.Yield();

            // 1. If any upstream is not Stable, exit.
            // 2. get Output branch
            // 3. get a clone of the repo
            // 4. if Output branch is behind one of the Upstream branches...
            //   4.a. If flow is automatic, attempt a merge. If it succeeds, go to 5.a, otherwise mark as conflicted and go to 5.b.
            //   4.b. If flow is not automatic, mark as needs update, and go to 5.b.
            // 5. See previous step for whether to run a or b
            //   5.a. Make a conflict branch
            //   5.b. Push output and then mark as stable

            if (!AreAllUpstreamBranchesStable(parameters.UpstreamReserves, logger))
            {
                return;
            }

            // TODO - this should be done via a lookup
            var strategy = (IFlowStrategy)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, parameters.Reserve.FlowType == "Automatic"
                ? typeof(AutomaticFlowStrategy)
                : typeof(ManualFlowStrategy)
            );

            using var repo = GitRepositoryUtilities.CloneAsLocal(options.CheckoutPath, parameters.WorkingPath, automationOptions);
            var prepared = PrepareOutputBranch(parameters, logger, repo, branchNaming);
            switch (prepared)
            {
                case null:
                    return;
                case (var push, var newOutputBranch, var outputBranchName):
                    var (pushAfterMerge, upstreamNeeded) = MergeUpstreamReserves(parameters, repo, strategy);
                    push = push || pushAfterMerge;

                    if (push)
                    {
                        if (!PushBranch(logger, repo, outputBranchName))
                        {
                            HandleFailedToPush(agent, parameters, outputBranchName);
                            break;
                        }
                    }

                    await HandleFinalState(logger, agent, parameters, repo, push, newOutputBranch, outputBranchName, upstreamNeeded, strategy);

                    break;
            }
        }

        private bool PushBranch(ILogger logger, Repository repo, string outputBranchName)
        {
            try
            {
                repo.EnsureRemoteAndPushBranch(outputBranchName, "HEAD", options, branchNaming);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Could not push branch {outputBranchName}", outputBranchName);
                return false;
            }

            return true;
        }

        private MultiMergeStatus MergeUpstreamReserves(ReserveScriptParameters parameters,  Repository repo, IFlowStrategy flowStrategy)
        {
            var gitIdentity = options.GitIdentity.ToGitIdentity();
            var result = new MultiMergeStatus(false);
            foreach (var upstream in parameters.UpstreamReserves.Keys)
            {
                // TODO - detect upstream history loss, where the tip that was merged is no longer in upstream, using reserve.Upstream[upstream].LastOutput. This probably goes to NeedsUpdate or other special state, and we should stop trying to merge anything.
                if (NeedsCommit(repo, parameters.UpstreamReserves[upstream].OutputCommit, out var commit))
                {
                    result = flowStrategy.Merge(repo, result, commit, upstream, gitIdentity);
                }
            }

            return result;
        }

        private async Task HandleFinalState(ILogger logger, IAgentSpecification agent, ReserveScriptParameters parameters, Repository repo, bool push, bool newOutputBranch, string outputBranchName, IEnumerable<string> upstreamNeeded, IFlowStrategy flowStrategy)
        {
            switch ((needsManual: upstreamNeeded.Any(), push))
            {
                case (needsManual: false, push: true):
                    HandlePushedOutput(agent, parameters, repo, newOutputBranch, outputBranchName);
                    break;
                case (needsManual: false, push: false):
                    HandleStabilizedReserve(agent, parameters, repo, outputBranchName);
                    break;
                case (needsManual: true, _):
                    await flowStrategy.DispatchManual(repo, parameters, logger, agent, outputBranchName, upstreamNeeded);
                    break;
                //case (needsManual: true, _) when parameters.Reserve.FlowType == "Automatic" && await ConflictsExistUpstream(repo, parameters, logger, agent):
                //    // TODO
                //    HandleUpstreamConflicts(agent, parameters);
                //    break;
                //default:
                //    HandleManualIntegration(agent, parameters, repo, outputBranchName, upstreamNeeded);
                //    break;
            }
        }

        private void HandlePushedOutput(IAgentSpecification agent, ReserveScriptParameters parameters, Repository repo, bool newOutputBranch, string outputBranchName)
        {
            var branchCommits = parameters.GetBranchExpectedOutputCommits()
                .Add(outputBranchName, repo.Head.Tip.Sha);
            var reserveOutputCommits = parameters.GetReserveExpectedOutputCommits();
            
            dispatcher.Dispatch(new StabilizePushedReserveAction
            {
                BranchCommits = branchCommits,
                NewOutput = newOutputBranch ? outputBranchName : null,
                Reserve = parameters.Name,
                ReserveOutputCommits = reserveOutputCommits
            }, agent, $"Auto-merge changes to '{outputBranchName}' for '{parameters.Name}'");
        }

        private void HandleStabilizedReserve(IAgentSpecification agent, ReserveScriptParameters parameters, Repository repo, string outputBranchName)
        {
            var branchCommits = parameters.GetBranchExpectedOutputCommits();
            var reserveOutputCommits = parameters.GetReserveExpectedOutputCommits();

            Debug.Assert(branchCommits[outputBranchName] == repo.Head.Tip.Sha);
            dispatcher.Dispatch(new StabilizeRemoteUpdatedReserveAction
            {
                BranchCommits = branchCommits,
                Reserve = parameters.Name,
                ReserveOutputCommits = reserveOutputCommits
            }, agent, $"No git changes needed for '{parameters.Name}'");
        }

        private void HandleFailedToPush(IAgentSpecification agent, ReserveScriptParameters parameters, string outputBranchName)
        {
            dispatcher.Dispatch(new CouldNotPushAction
            {
                BranchCommits = ImmutableDictionary<string, string>.Empty,
                ReserveOutputCommits = parameters.GetReserveExpectedOutputCommits(),
                Reserve = parameters.Name
            }, agent, $"Failed to push to '{outputBranchName}' for '{parameters.Name}'");
        }

        private static bool NeedsCommit(Repository repo, string outputCommit, out Commit commit)
        {
            commit = repo.Lookup<Commit>(outputCommit);
            var history = commit == null ? null : repo.ObjectDatabase.CalculateHistoryDivergence(repo.Head.Tip, commit);
            return history?.BehindBy != 0;
        }

    }
}
