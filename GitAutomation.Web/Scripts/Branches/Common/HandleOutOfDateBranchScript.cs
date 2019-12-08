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
    public class HandleOutOfDateBranchScript : IScript<ReserveScriptParameters>
    {
        private readonly IDispatcher dispatcher;
        private readonly TargetRepositoryOptions options;
        private readonly AutomationOptions automationOptions;

        public HandleOutOfDateBranchScript(IDispatcher dispatcher, IOptions<TargetRepositoryOptions> options, IOptions<AutomationOptions> automationOptions)
        {
            this.dispatcher = dispatcher;
            this.options = options.Value;
            this.automationOptions = automationOptions.Value;
        }

        public async Task Run(ReserveScriptParameters parameters, ILogger logger, IAgentSpecification agent)
        {
            await Task.Yield();
            var name = parameters.Name;
            var reserve = parameters.ReserveFullState.Reserve;
            var branchDetails = parameters.ReserveFullState.BranchDetails;
            var upstreamReserves = parameters.ReserveFullState.UpstreamReserves;
            var remotes = options.Remotes;
            var checkoutPath = options.CheckoutPath;
            var workingPath = parameters.WorkingPath;
            var defaultRemote = automationOptions.DefaultRemote;
            var integrationPrefix = automationOptions.IntegrationPrefix;
            var userEmail = options.UserEmail;
            var userName = options.UserName;

            // 1. If any upstream is not Stable, exit.
            // 2. get Output branch
            // 3. get a clone of the repo
            // 4. if Output branch is behind one of the Upstream branches...
            //   4.a. If flow is automatic, attempt a merge. If it succeeds, go to 5.a, otherwise mark as conflicted and go to 5.b.
            //   4.b. If flow is not automatic, mark as needs update, and go to 5.b.
            // 5. See previous step for whether to run a or b
            //   5.a. Make a conflict branch
            //   5.b. Push output and then mark as stable

            if (upstreamReserves.Any(r => r.Value.Status != "Stable"))
            {
                logger.LogInformation($"Had upstream reserves ({string.Join(", ", upstreamReserves.Where(r => r.Value.Status != "Stable").Select(r => r.Key))}) in non-Stable state. Deferring.", upstreamReserves.Where(r => r.Value.Status != "Stable").ToArray());
                return;
            }
            var outputBranches = reserve.IncludedBranches.Keys.Where(k => reserve.IncludedBranches[k].Meta["Role"] == "Output").ToArray();
            if (outputBranches.Length > 1)
            {
                logger.LogError($"Had multiple output branches: {string.Join(", ", outputBranches)}", outputBranches);
                return;
            }
            var (newOutputBranch, outputBranchName) =
                outputBranches.SingleOrDefault() switch
                {
                    null => (true, "foo"),
                    var value => (false, value),
                };

            var push = false;
            Repository.Clone(checkoutPath, workingPath);
            // TODO - clone'd remote should be using automationOptions.WorkingRemote, but is probably origin
            using var repo = new Repository(workingPath);
            if (newOutputBranch)
            {
                if (repo.Branches[outputBranchName] != null)
                {
                    logger.LogError($"Default output branch '{outputBranchName}' already exists but was not allocated to reserve.", outputBranchName);
                    return;
                }
                push = true;
                Commands.Checkout(repo, upstreamReserves.First().Value.OutputCommit);
            }
            else
            {
                Commands.Checkout(repo, outputBranchName);
            }

            var finalState = "Stable";
            var upstreamNeeded = new List<string>();
            foreach (var upstream in upstreamReserves.Keys)
            {
                var commit = repo.Lookup<Commit>(upstreamReserves[upstream].OutputCommit);
                var history = repo.ObjectDatabase.CalculateHistoryDivergence(repo.Head.Tip, commit);
                if (history.BehindBy > 0)
                {
                    if (reserve.FlowType != "Automatic")
                    {
                        finalState = "NeedsUpdate";
                        upstreamNeeded.Add(upstream);
                    }
                    else
                    {
                        var message = $"Auto-merge {upstream}";
                        var result = repo.Merge(commit, new Signature(userName, userEmail, DateTimeOffset.Now), new MergeOptions
                        {
                            CommitOnSuccess = false
                        });
                        switch (result.Status)
                        {
                            case MergeStatus.Conflicts:
                                finalState = "Conflicted";
                                repo.Reset(ResetMode.Hard);
                                upstreamNeeded.Add(upstream);
                                break;
                            case MergeStatus.FastForward:
                            case MergeStatus.UpToDate:
                                break;
                            case MergeStatus.NonFastForward:
                                repo.Commit(message, new Signature(userName, userEmail, DateTimeOffset.Now), new Signature(userName, userEmail, DateTimeOffset.Now));
                                push = true;
                                break;
                        }
                    }
                }
            }

            var branchParts = outputBranchName.Split('/', 2);
            var baseRemote = branchParts[0];
            var outputBranchRemoteName = branchParts[1];

            if (push)
            {
                try
                {
                    repo.Network.Push(repo.Network.Remotes[baseRemote], $"HEAD:refs/heads/{outputBranchRemoteName}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Could not push to {baseRemote} branch {outputBranchRemoteName}", baseRemote, outputBranchRemoteName);
                    finalState = "CouldNotPush";
                }
            }

            var branchCommits = branchDetails
                .Where(b => b.Value != reserve.IncludedBranches[b.Key].LastCommit)
                .ToDictionary(b => b.Key, b => b.Value);
            var reserveOutputCommits = upstreamReserves
                .Where(r => r.Value.OutputCommit != reserve.Upstream[r.Key].LastOutput)
                .ToDictionary(r => r.Key, r => upstreamReserves[r.Key].OutputCommit);

            switch (finalState)
            {
                case "Stable":
                    branchCommits[outputBranchName] = repo.Head.Tip.Sha;
                    dispatcher.Dispatch(new StabilizePushedReserveAction
                    {
                        BranchCommits = branchCommits,
                        NewOutput = newOutputBranch ? outputBranchName : null,
                        Reserve = name,
                        ReserveOutputCommits = reserveOutputCommits
                    }, agent, push ? $"Auto-merge changes to '{outputBranchName}' for '{name}'" : $"No git changes needed for '{name}'");
                    break;
                case "CouldNotPush":
                    dispatcher.Dispatch(new CouldNotPushAction
                    {
                        BranchCommits = branchCommits,
                        ReserveOutputCommits = reserveOutputCommits,
                        Reserve = name
                    }, agent, $"Failed to push to '{outputBranchName}' for '{name}'");
                    break;
                case "NeedsUpdate":
                case "Conflicted":
                    var newBranches = upstreamNeeded.Select(entry =>
                    {
                        var commit = upstreamReserves[entry].OutputCommit;
                        var newBranchName = integrationPrefix + name + "/" + entry;
                        var remoteBranchName = $"{defaultRemote}/{newBranchName}";
                        if (repo.Branches[remoteBranchName] != null && (!reserve.IncludedBranches.ContainsKey(remoteBranchName) || reserve.IncludedBranches[remoteBranchName].Meta["Role"] != "Integration"))
                        {
                            return default;
                        }
                        return new ManualInterventionNeededAction.ManualInterventionBranch
                        {
                            Commit = commit,
                            Name = remoteBranchName,
                            Role = "Integration",
                            Source = entry
                        };
                    }).Where(b => b.Name != null).ToArray();
                    var refspecs = string.Join(' ', newBranches.Select(b => $"{b.Commit}:refs/heads/${b.Name.Substring(defaultRemote.Length + 1)}"));
                    // TODO - credentials!
                    repo.Network.Push(repo.Network.Remotes[defaultRemote], refspecs);

                    dispatcher.Dispatch(new ManualInterventionNeededAction
                    {
                        BranchCommits = branchCommits,
                        ReserveOutputCommits = reserveOutputCommits,
                        Reserve = name,
                        State = finalState,
                        NewBranches = newBranches,
                    }, agent, $"Need manual merging to '{outputBranchName}' for '{name}'");
                    break;
            }
        }
    }
}
