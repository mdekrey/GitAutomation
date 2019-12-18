using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Actions;
using GitAutomation.Scripting;
using GitAutomation.Web;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Branches.Common
{
    public class HandleOutOfDateBranchScript : IScript<ReserveScriptParameters>
    {
        private readonly IDispatcher dispatcher;
        private readonly IBranchNaming branchNaming;
        private readonly IIntegrationReserveUtilities integrationReserveUtilities;
        private readonly TargetRepositoryOptions options;
        private readonly AutomationOptions automationOptions;
        protected static readonly MergeOptions standardMergeOptions = new MergeOptions
        {
            CommitOnSuccess = false
        };

        public HandleOutOfDateBranchScript(IDispatcher dispatcher, IOptions<TargetRepositoryOptions> options, IOptions<AutomationOptions> automationOptions, IBranchNaming branchNaming, IIntegrationReserveUtilities integrationReserveUtilities)
        {
            this.dispatcher = dispatcher;
            this.branchNaming = branchNaming;
            this.integrationReserveUtilities = integrationReserveUtilities;
            this.options = options.Value;
            this.automationOptions = automationOptions.Value;
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

            if (parameters.UpstreamReserves.Any(r => r.Value.Status != "Stable"))
            {
                logger.LogInformation($"Had upstream reserves ({string.Join(", ", parameters.UpstreamReserves.Where(r => r.Value.Status != "Stable").Select(r => r.Key))}) in non-Stable state. Deferring.", parameters.UpstreamReserves.Where(r => r.Value.Status != "Stable").ToArray());
                return;
            }
            using var repo = CloneAsLocal(options.CheckoutPath, parameters.WorkingPath);
            var prepared = PrepareOutputBranch(parameters, logger, repo);
            switch (prepared)
            {
                case null:
                    return;
                case (var push, var newOutputBranch, var outputBranchName):
                    var (pushAfterMerge, finalState, upstreamNeeded) = MergeUpstreamReserves(parameters, repo);
                    push = push || pushAfterMerge;

                    if (push)
                    {
                        var (baseRemote, outputBranchRemoteName) = branchNaming.SplitCheckoutRepositoryBranchName(outputBranchName);
                        try
                        {
                            EnsureRemoteAndPush(repo, baseRemote, $"HEAD:refs/heads/{outputBranchRemoteName}");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Could not push to {baseRemote} branch {outputBranchRemoteName}", baseRemote, outputBranchRemoteName);
                            finalState = "CouldNotPush";
                        }
                    }

                    await HandleFinalState(logger, agent, parameters, repo, push, newOutputBranch, outputBranchName, finalState, upstreamNeeded);

                    break;
            }
        }

        private (bool push, bool newOutputBranch, string outputBranchName)? PrepareOutputBranch(ReserveScriptParameters parameters, ILogger logger, Repository repo)
        {
            var outputBranches = GetOutputBranches(parameters.Reserve);
            var (newOutputBranch, outputBranchName) =
                outputBranches.Length switch
                {
                    0 => (true, branchNaming.GetCheckoutRepositoryBranchName(automationOptions.DefaultRemote, parameters.Name)),
                    1 => (false, outputBranches[0]),
                    _ => (false, null),
                };

            var push = false;
            if (outputBranchName == null)
            {
                logger.LogError($"Had multiple output branches: {string.Join(", ", outputBranches)}", outputBranches);
                return null;
            }

            if (newOutputBranch)
            {
                if (repo.Branches[outputBranchName] != null)
                {
                    logger.LogError($"Default output branch '{outputBranchName}' already exists but was not allocated to reserve.", outputBranchName);
                    return null;
                }
                push = true;
                Commands.Checkout(repo, parameters.UpstreamReserves.First().Value.OutputCommit);
            }
            else
            {
                Commands.Checkout(repo, outputBranchName);
            }

            return (push, newOutputBranch, outputBranchName);
        }

        private (bool push, string finalState, List<string> upstreamNeeded) MergeUpstreamReserves(ReserveScriptParameters parameters,  Repository repo)
        {
            var gitIdentity = options.GitIdentity.ToGitIdentity();
            var push = false;
            var finalState = "Stable";
            var upstreamNeeded = new List<string>();
            foreach (var upstream in parameters.UpstreamReserves.Keys)
            {
                if (NeedsCommit(repo, parameters.UpstreamReserves[upstream].OutputCommit, out var commit))
                {
                    // TODO - detect upstream history loss, where the tip that was merged is no longer in upstream, using reserve.Upstream[upstream].LastOutput. This probably goes to NeedsUpdate or other special state, and we should stop trying to merge anything.
                    if (parameters.Reserve.FlowType != "Automatic")
                    {
                        finalState = "NeedsUpdate";
                        upstreamNeeded.Add(upstream);
                    }
                    else
                    {
                        var message = $"Auto-merge {upstream}";
                        var result = repo.Merge(commit, new Signature(gitIdentity, DateTimeOffset.Now), standardMergeOptions);
                        switch (result.Status)
                        {
                            case MergeStatus.Conflicts:
                                finalState = "Conflicted";
                                ResetAndClean(repo);
                                upstreamNeeded.Add(upstream);
                                break;
                            case MergeStatus.FastForward:
                            case MergeStatus.UpToDate:
                                break;
                            case MergeStatus.NonFastForward:
                                repo.Commit(message, new Signature(gitIdentity, DateTimeOffset.Now), new Signature(gitIdentity, DateTimeOffset.Now));
                                push = true;
                                break;
                        }
                    }
                }
            }

            return (push, finalState, upstreamNeeded);
        }

        private static void ResetAndClean(Repository repo)
        {
            repo.Reset(ResetMode.Hard);
            foreach (var entry in repo.RetrieveStatus())
            {
                if (entry.State != FileStatus.Unaltered)
                {
                    System.IO.File.Delete(System.IO.Path.Combine(repo.Info.Path, entry.FilePath));
                }
            }
        }

        private async Task HandleFinalState(ILogger logger, IAgentSpecification agent, ReserveScriptParameters parameters, Repository repo, bool push, bool newOutputBranch, string outputBranchName, string finalState, List<string> upstreamNeeded)
        {
            var branchCommits = parameters.BranchDetails.Where(b => b.Value != parameters.Reserve.IncludedBranches[b.Key].LastCommit)
                                    .ToDictionary(b => b.Key, b => b.Value);
            var reserveOutputCommits = parameters.UpstreamReserves.Where(r => r.Value.OutputCommit != parameters.Reserve.Upstream[r.Key].LastOutput)
                .ToDictionary(r => r.Key, r => parameters.UpstreamReserves[r.Key].OutputCommit);

            switch (finalState)
            {
                case "Stable":
                    if (push)
                    {
                        HandlePushedOutput(agent, parameters.Name, repo, newOutputBranch, outputBranchName, branchCommits, reserveOutputCommits);
                    }
                    else
                    {
                        HandleStabilizedReserve(agent, parameters.Name, repo, outputBranchName, branchCommits, reserveOutputCommits);
                    }
                    break;
                case "CouldNotPush":
                    HandleFailedToPush(agent, parameters.Name, outputBranchName, branchCommits, reserveOutputCommits);
                    break;
                case "NeedsUpdate":
                    HandleManualIntegration(agent, parameters, repo, outputBranchName, finalState, upstreamNeeded, branchCommits, reserveOutputCommits);
                    break;
                case "Conflicted":
                    if (await ConflictsExistUpstream(repo, parameters, logger, agent))
                    {
                        HandleUpstreamConflicts(agent, parameters.Name, branchCommits, reserveOutputCommits);
                    }
                    else
                    {
                        HandleManualIntegration(agent, parameters, repo, outputBranchName, finalState, upstreamNeeded, branchCommits, reserveOutputCommits);
                    }
                    break;
            }
        }

        private void HandlePushedOutput(IAgentSpecification agent, string name, Repository repo, bool newOutputBranch, string outputBranchName, Dictionary<string, string> branchCommits, Dictionary<string, string> reserveOutputCommits)
        {
            branchCommits[outputBranchName] = repo.Head.Tip.Sha;
            dispatcher.Dispatch(new StabilizePushedReserveAction
            {
                BranchCommits = branchCommits,
                NewOutput = newOutputBranch ? outputBranchName : null,
                Reserve = name,
                ReserveOutputCommits = reserveOutputCommits
            }, agent, $"Auto-merge changes to '{outputBranchName}' for '{name}'");
        }

        private void HandleStabilizedReserve(IAgentSpecification agent, string name, Repository repo, string outputBranchName, Dictionary<string, string> branchCommits, Dictionary<string, string> reserveOutputCommits)
        {
            branchCommits[outputBranchName] = repo.Head.Tip.Sha;
            dispatcher.Dispatch(new StabilizeRemoteUpdatedReserveAction
            {
                BranchCommits = branchCommits,
                Reserve = name,
                ReserveOutputCommits = reserveOutputCommits
            }, agent, $"No git changes needed for '{name}'");
        }

        private void HandleFailedToPush(IAgentSpecification agent, string name, string outputBranchName, Dictionary<string, string> branchCommits, Dictionary<string, string> reserveOutputCommits)
        {
            dispatcher.Dispatch(new CouldNotPushAction
            {
                BranchCommits = branchCommits,
                ReserveOutputCommits = reserveOutputCommits,
                Reserve = name
            }, agent, $"Failed to push to '{outputBranchName}' for '{name}'");
        }

        private ManualInterventionNeededAction.ManualInterventionBranch[] CreateTemporaryIntegrationBranches(ReserveScriptParameters parameters, Repository repo, List<string> upstreamNeeded)
        {
            var defaultRemote = automationOptions.DefaultRemote;

            // TODO - temporary integration branches should be removed somewhere - maybe when it becomes stable?
            var newBranches = upstreamNeeded.Select(entry =>
            {
                var commit = parameters.UpstreamReserves[entry].OutputCommit;
                var newBranchName = branchNaming.GenerateIntegrationBranchName(parameters.Name, entry);
                var remoteBranchName = branchNaming.GetCheckoutRepositoryBranchName(defaultRemote, newBranchName);
                if (repo.Branches[remoteBranchName] != null && (!parameters.Reserve.IncludedBranches.ContainsKey(remoteBranchName) || parameters.Reserve.IncludedBranches[remoteBranchName].Meta["Role"] != "Integration"))
                {
                    // TODO - should warn or something here
                    return default;
                }
                repo.CreateBranch(remoteBranchName, commit);
                return new ManualInterventionNeededAction.ManualInterventionBranch
                {
                    Commit = commit,
                    Name = remoteBranchName,
                    Role = "Integration",
                    Source = entry
                };
            }).Where(b => b.Name != null).ToArray();
            var refspecs = newBranches.Select(b => $"refs/heads/{b.Name}:refs/heads/{b.Name.Substring(defaultRemote.Length + 1)}").ToArray();
            EnsureRemoteAndPush(repo, defaultRemote, refspecs);
            return newBranches;
        }

        private void HandleManualIntegration(IAgentSpecification agent, ReserveScriptParameters parameters, Repository repo, string outputBranchName, string finalState, List<string> upstreamNeeded, Dictionary<string, string> branchCommits, Dictionary<string, string> reserveOutputCommits)
        {
            var newBranches = CreateTemporaryIntegrationBranches(parameters, repo, upstreamNeeded);
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
                State = finalState,
                NewBranches = newBranches,
            }, agent, message);
        }

        private void HandleUpstreamConflicts(IAgentSpecification agent, string name, Dictionary<string, string> branchCommits, Dictionary<string, string> reserveOutputCommits)
        {
            // TODO - ManualInterventionNeededAction isn't really the best here, but it works
            dispatcher.Dispatch(new ManualInterventionNeededAction
            {
                BranchCommits = branchCommits,
                ReserveOutputCommits = reserveOutputCommits,
                Reserve = name,
                State = "OutOfDate",
                NewBranches = new ManualInterventionNeededAction.ManualInterventionBranch[0],
            }, agent, $"Conflicts detected upstream from '{name}'");
        }

        private static string[] GetOutputBranches(BranchReserve reserve)
        {
            return reserve.IncludedBranches.Keys.Where(k => reserve.IncludedBranches[k].Meta["Role"] == "Output").ToArray();
        }

        private Repository CloneAsLocal(string checkoutPath, string workingPath)
        {
            Repository.Init(workingPath, isBare: false);
            var repo = new Repository(workingPath);
            repo.Network.Remotes.Add(automationOptions.WorkingRemote, checkoutPath);
            Commands.Fetch(repo, automationOptions.WorkingRemote, new[] { "refs/heads/*:refs/heads/*" }, new FetchOptions { }, "");
            return repo;
        }

        private bool NeedsCommit(Repository repo, string outputCommit, out Commit commit)
        {
            commit = repo.Lookup<Commit>(outputCommit);
            var history = commit == null ? null : repo.ObjectDatabase.CalculateHistoryDivergence(repo.Head.Tip, commit);
            return history?.BehindBy != 0;
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

        private void EnsureRemoteAndPush(Repository repo, string remoteName, params string[] refSpecs)
        {
            if (repo.Network.Remotes[remoteName] == null)
            {
                repo.Network.Remotes.Add(remoteName, options.Remotes[remoteName].Url);
            }
            repo.Network.Push(repo.Network.Remotes[remoteName], refSpecs, new PushOptions
            {
                CredentialsProvider = options.Remotes[remoteName].ToCredentialsProvider()
            });
        }
    }
}
