﻿using GitAutomation.BranchSettings;
using GitAutomation.GitService;
using GitAutomation.Orchestration.Actions.MergeStrategies;
using GitAutomation.Processes;
using GitAutomation.Repository;
using GitAutomation.Work;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitAutomation.Orchestration.Actions
{
    class MergeDownstreamAction : ComplexUniqueAction<MergeDownstreamAction.MergeDownstreamActionProcess>
    {
        private readonly string downstreamBranch;

        public override string ActionType => "MergeDownstream";
        public string DownstreamBranch => downstreamBranch;

        public MergeDownstreamAction(string downstreamBranch)
        {
            this.downstreamBranch = downstreamBranch;
        }

        public override JToken Parameters => JToken.FromObject(new Dictionary<string, string>
            {
                { "downstreamBranch", downstreamBranch },
            }.ToImmutableDictionary());

        internal override object[] GetExtraParameters()
        {
            return new object[] { downstreamBranch };
        }
        

        public class MergeDownstreamActionProcess : ComplexActionInternal
        {
            private readonly IGitCli cli;
            private readonly IGitServiceApi gitServiceApi;
            private readonly IntegrateBranchesOrchestration integrateBranches;
            private readonly IRepositoryMediator repository;
            private readonly IRepositoryOrchestration orchestration;
            private readonly IBranchIterationMediator branchIteration;
            private readonly string downstreamBranchGroup;
            private readonly Task<BranchGroupCompleteData> detailsTask;
            private readonly Task<string> latestBranchName;
            private readonly Task<IMergeStrategy> strategyTask;
            private readonly bool isReadOnly;

            private BranchGroupCompleteData Details => detailsTask.Result;
            private string LatestBranchName => latestBranchName.Result;
            private IMergeStrategy Strategy => strategyTask.Result;

            public MergeDownstreamActionProcess(IGitCli cli, IGitServiceApi gitServiceApi, IUnitOfWorkFactory workFactory, IRepositoryOrchestration orchestration, IRepositoryMediator repository, IntegrateBranchesOrchestration integrateBranches, IBranchIterationMediator branchIteration, string downstreamBranch, IOptions<GitRepositoryOptions> options, IMergeStrategyManager strategyManager)
            {
                this.cli = cli;
                this.gitServiceApi = gitServiceApi;
                this.integrateBranches = integrateBranches;
                this.repository = repository;
                this.orchestration = orchestration;
                this.branchIteration = branchIteration;
                this.downstreamBranchGroup = downstreamBranch;
                this.detailsTask = repository.GetBranchDetails(downstreamBranch).FirstAsync().ToTask();
                this.latestBranchName = detailsTask.ContinueWith(task => repository.LatestBranchName(task.Result).FirstOrDefaultAsync().ToTask()).Unwrap();
                this.strategyTask = detailsTask.ContinueWith(task => strategyManager.GetMergeStrategy(task.Result));
                this.isReadOnly = options.Value.ReadOnly;
            }

            protected override async Task RunProcess()
            {
                if (isReadOnly)
                {
                    return;
                }
                
                await detailsTask;
                await latestBranchName;

                var allConfigured = await repository.GetConfiguredBranchGroups().FirstOrDefaultAsync();
                var upstreamBranches = (await ToUpstreamBranchNames(Details.DirectUpstreamBranchGroups.Select(groupName => allConfigured.Find(g => g.GroupName == groupName)).ToImmutableList())).ToImmutableList();
                var needsCreate = await Strategy.NeedsCreate(LatestBranchName, upstreamBranches);
                var neededUpstreamMerges = await Strategy.FindNeededMerges(LatestBranchName, upstreamBranches);
                
                if (needsCreate)
                {
                    await AppendMessage($"{Details.GroupName} needs a new branch to be created from {string.Join(",", neededUpstreamMerges.Select(up => up.GroupName))}");
                    var (created, branchName) = await CreateDownstreamBranch(upstreamBranches);
                    if (created)
                    {
                        await PushBranch(branchName);
                        await Strategy.AfterCreate(Details, LatestBranchName, branchName, AppendProcess);
                    }
                }
                else if (neededUpstreamMerges.Any())
                {
                    await AppendMessage($"{LatestBranchName} needs merges from {string.Join(",", neededUpstreamMerges.Select(up => up.GroupName))}");
                    var merged = await MergeToBranch(LatestBranchName, neededUpstreamMerges);

                    if (merged)
                    {
                        await PushBranch(LatestBranchName);
                    }
                }
            }

            private async Task<bool> MergeToBranch(string latestBranchName, ImmutableList<NeededMerge> neededUpstreamMerges)
            {
                await AppendProcess(cli.CheckoutRemote(LatestBranchName));

                var shouldPush = false;
                foreach (var upstreamBranch in neededUpstreamMerges)
                {
                    var result = await MergeUpstreamBranch(upstreamBranch, LatestBranchName);
                    shouldPush = shouldPush || !result.HadConflicts;
                    if (result.HadConflicts)
                    {
                        await AppendProcess(cli.Checkout(LatestBranchName));

                    }
                }

                return shouldPush;
            }

            private async Task<IEnumerable<NeededMerge>> ToUpstreamBranchNames(ImmutableList<BranchGroup> directUpstreamBranchGroups)
            {
                var hierarchy = (await repository.AllBranchesHierarchy().Take(1)).ToDictionary(branch => branch.GroupName, branch => branch.HierarchyDepth);
                var remotes = await repository.GetAllBranchRefs().FirstOrDefaultAsync();
                // Put integration branches first; they might be needed for conflict resolution in other branches!
                // FIXME - this is using the GroupName rather than the BranchName!
                return from branch in directUpstreamBranchGroups
                       orderby branch.BranchType == BranchGroupType.Integration ? 0 : 1, -hierarchy[branch.GroupName], branch.GroupName
                       select new NeededMerge
                       {
                           GroupName = branch.GroupName,
                           BranchName = branchIteration.GetLatestBranchNameIteration(branch.GroupName, remotes.Select(r => r.Name))
                       };
            }
            
            private async Task<(bool created, string branchName)> CreateDownstreamBranch(IEnumerable<NeededMerge> allUpstreamBranches)
            {
                var downstreamBranch = await repository.GetNextCandidateBranch(Details).FirstOrDefaultAsync();
                var validUpstream = allUpstreamBranches.ToImmutableList();
                if (validUpstream.Count == 0 || validUpstream.Any(t => t.BranchName == null))
                {
                    if (validUpstream.Any(t => t.BranchName != null))
                    {
#pragma warning disable CS4014
                        orchestration.EnqueueAction(new MergeDownstreamAction(validUpstream.First(t => t.BranchName == null).GroupName), skipDuplicateCheck: false);
#pragma warning restore
                        await AppendMessage($"{validUpstream.First(t => t.BranchName == null).GroupName} did not have current branch; aborting", isError: true);
                    }

                    return (created: false, branchName: null);
                }

                var badBranches = (await Task.WhenAll(
                    validUpstream.Select(t => repository.IsBadBranch(t.BranchName)
                        .ContinueWith(task => new { IsBad = task.Result, BranchName = t.BranchName })
                    )
                )).Where(b => b.IsBad);
                if (badBranches.Any())
                {
                    await AppendMessage($"{badBranches.First().BranchName} is marked as bad; aborting", isError: true);
                    return (created: false, branchName: null);
                }

                // Basic process; should have checks on whether or not to create the branch
                var initialBranch = validUpstream[0];

                var checkout = cli.CheckoutRemote(initialBranch.BranchName);
                var checkoutError = await checkout.FirstErrorMessage();
                await AppendProcess(checkout);
                if (!string.IsNullOrEmpty(checkoutError) && checkoutError.StartsWith("fatal"))
                {
                    await AppendMessage( $"{downstreamBranch} unable to be branched from {initialBranch.BranchName}; aborting" );
                    return (created: false, branchName: null);
                }

                await AppendProcess(cli.CheckoutNew(downstreamBranch));

                var shouldCancel = false;
                var shouldRetry = false;
                foreach (var upstreamBranch in validUpstream.Skip(1))
                {
                    var result = await MergeUpstreamBranch(upstreamBranch, downstreamBranch);
                    shouldCancel = shouldCancel || result.HadConflicts;
                    shouldRetry = shouldRetry || (result.HadConflicts && result.Resolution == MergeConflictResolution.AddIntegrationBranch);
                }
                if (shouldCancel)
                {
                    if (shouldRetry)
                    {
                        // abort, but queue another attempt
#pragma warning disable CS4014
                        orchestration.EnqueueAction(new MergeDownstreamAction(downstreamBranchGroup), skipDuplicateCheck: true);
#pragma warning restore
                    }
                    return (created: false, branchName: null);
                }

                return (created: true, branchName: downstreamBranch);
            }

            private async Task PushBranch(string downstreamBranch)
            {
                var pushProcess = cli.Push(downstreamBranch);
                var pushExitCode = await pushProcess.ExitCode();
                await AppendProcess(pushProcess);
                if (pushExitCode == 0)
                {
                    var newValue = await cli.ShowRef(downstreamBranch).FirstOutputMessage();
                    await repository.BranchUpdated(downstreamBranch, newValue, await repository.GetBranchRef(downstreamBranch).FirstOrDefaultAsync());
                    // Makes sure the local repository is updated with the new branch value
                    await repository.GetBranchRef(downstreamBranch).Where(v => v == newValue).FirstOrDefaultAsync();
                }
            }
            
            /// <summary>
            /// Notice - assumes that downstream is already checked out!
            /// </summary>
            private async Task<MergeStatus> MergeUpstreamBranch(NeededMerge upstreamBranch, string downstreamBranch)
            {
                bool isSuccessfulMerge = await DoMerge(upstreamBranch.BranchName, downstreamBranch, message: $"Auto-merge branch '{upstreamBranch.GroupName}'");
                if (isSuccessfulMerge)
                {
                    return new MergeStatus { HadConflicts = false };
                }

                // conflict!
                await AppendMessage($"{downstreamBranch} conflicts with {upstreamBranch.GroupName}", isError: true);

                

                var canUseIntegrationBranch = Details.BranchType != BranchGroupType.Integration;
                var createdIntegrationBranch = canUseIntegrationBranch
                    ? Details.DirectUpstreamBranchGroups.Count != 1 
                        ? await integrateBranches.FindAndCreateIntegrationBranches(
                                Details,
                                Details.DirectUpstreamBranchGroups, 
                                DoMergeWithCheckout
                            )
                        : await integrateBranches.FindSingleIntegrationBranch(
                                Details,
                                upstreamBranch.GroupName,
                                DoMergeWithCheckout
                            )
                    : (IntegrationBranchResult?)null;

                if (createdIntegrationBranch.HasValue)
                {
                    if (createdIntegrationBranch?.Conflicts?.Any() ?? false)
                    {
                        await AppendMessage($"Detected conflicts:\n" + string.Join("\n", createdIntegrationBranch.Value.Conflicts.Select(c => $"{c.BranchA.LatestBranchName} and {c.BranchB.LatestBranchName}")), isError: true);
                    }

                    if (createdIntegrationBranch?.AddedBranches?.Any() ?? false)
                    {
                        await AppendMessage($"Added branches:\n" + string.Join("\n", createdIntegrationBranch.Value.AddedBranches), isError: true);
                    }
                }

                if (!createdIntegrationBranch.HasValue || createdIntegrationBranch.Value.NeedsPullRequest())
                {
                    repository.FlagBadGitRef(downstreamBranch, await cli.ShowRef(downstreamBranch).FirstOutputMessage());
                    if (await gitServiceApi.HasOpenPullRequest(targetBranch: downstreamBranch, sourceBranch: upstreamBranch.BranchName))
                    {
                        return new MergeStatus { HadConflicts = true, Resolution = MergeConflictResolution.PendingPullRequest };
                    }
                    else
                    {
                        // Open a PR if we can't open a new integration branch
                        await PushBranch(downstreamBranch);
                        await gitServiceApi.OpenPullRequest(
                            title: $"Auto-Merge: {downstreamBranch}", 
                            targetBranch: downstreamBranch, 
                            sourceBranch: upstreamBranch.BranchName, 
                            body: @"Failed due to merge conflicts. Don't use web resolution. Instead:

    git fetch
    git checkout -B " + downstreamBranch + @" --track origin/" + downstreamBranch + @"
    git merge origin/" + upstreamBranch.BranchName + @"

This will cause the relevant conflicts to be able to resolved in your editor of choice. Once you have resolved, make sure you add them to the index, commit and push.
");
                        return new MergeStatus { HadConflicts = true, Resolution = MergeConflictResolution.PullRequest };
                    }
                }
                else if (createdIntegrationBranch.Value.Resolved)
                {
                    return new MergeStatus
                    {
                        HadConflicts = false,
                    };
                }
                else
                {
                    await AppendMessage($"{downstreamBranch} had integration branches added.", isError: true);
                    return new MergeStatus {
                        HadConflicts = true,
                        Resolution = createdIntegrationBranch.Value.AddedNewIntegrationBranches ? MergeConflictResolution.AddIntegrationBranch : MergeConflictResolution.PendingIntegrationBranch
                    };
                }
            }

            private async Task CleanIndex()
            {
                await AppendProcess(cli.Reset());

                await AppendProcess(cli.Clean());
            }


            private async Task<bool> DoMergeWithCheckout(string upstreamBranch, string targetBranch, string message)
            {
                await AppendProcess(cli.CheckoutRemote(targetBranch));

                return await DoMerge(upstreamBranch, targetBranch, message);
            }


            private async Task<bool> DoMerge(string upstreamBranch, string targetBranch, string message)
            {
                var timestamps = (from timestampMessage in (await AppendProcess(cli.GetCommitTimestamps(cli.RemoteBranch(upstreamBranch), targetBranch))).Process.Output
                                        where timestampMessage.Channel == OutputChannel.Out
                                        select timestampMessage.Message).ToArray();
                var timestamp = timestamps.Max();

                var mergeExitCode = (await AppendProcess(cli.MergeRemote(upstreamBranch, message, commitDate: timestamp))).Process.ExitCode;
                var isSuccessfulMerge = mergeExitCode == 0;
                await CleanIndex();
                return isSuccessfulMerge;
            }
        }
    }
}
