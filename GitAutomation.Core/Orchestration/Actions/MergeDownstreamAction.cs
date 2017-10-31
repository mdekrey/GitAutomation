using GitAutomation.BranchSettings;
using GitAutomation.GitService;
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
    class MergeDownstreamAction : IRepositoryAction
    {
        private enum MergeConflictResolution
        {
            AddIntegrationBranch,
            PendingIntegrationBranch,
            PullRequest,
            PendingPullRequest,
        }

        private struct MergeStatus
        {
            public bool HadConflicts;
            public MergeConflictResolution Resolution;
        }

        private struct NeededMerge
        {
            public string GroupName;
            public string BranchName;
        }

        private readonly Subject<OutputMessage> output = new Subject<OutputMessage>();
        private readonly string downstreamBranch;

        public string ActionType => "MergeDownstream";
        public string DownstreamBranch => downstreamBranch;

        public MergeDownstreamAction(string downstreamBranch)
        {
            this.downstreamBranch = downstreamBranch;
        }

        public JToken Parameters => JToken.FromObject(new Dictionary<string, string>
            {
                { "downstreamBranch", downstreamBranch },
            }.ToImmutableDictionary());

        public IObservable<OutputMessage> DeferredOutput => output;

        public IObservable<OutputMessage> PerformAction(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<MergeDownstreamActionProcess>(serviceProvider, downstreamBranch).Process().Multicast(output).RefCount();
        }

        private class MergeDownstreamActionProcess : ComplexAction
        {
            private readonly GitCli cli;
            private readonly IGitServiceApi gitServiceApi;
            private readonly IntegrateBranchesOrchestration integrateBranches;
            private readonly IRepositoryMediator repository;
            private readonly IRepositoryOrchestration orchestration;
            private readonly IBranchIterationMediator branchIteration;
            private readonly string downstreamBranchGroup;
            private readonly Task<BranchGroupCompleteData> detailsTask;
            private readonly Task<string> latestBranchName;
            private readonly bool isReadOnly;

            private BranchGroupCompleteData Details => detailsTask.Result;
            private string LatestBranchName => latestBranchName.Result;

            public MergeDownstreamActionProcess(GitCli cli, IGitServiceApi gitServiceApi, IUnitOfWorkFactory workFactory, IRepositoryOrchestration orchestration, IRepositoryMediator repository, IntegrateBranchesOrchestration integrateBranches, IBranchIterationMediator branchIteration, string downstreamBranch, IOptions<GitRepositoryOptions> options)
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
                this.isReadOnly = options.Value.ReadOnly;
            }

            protected override async Task RunProcess()
            {
                if (isReadOnly)
                {
                    return;
                }
                // if these two are different, we need to do the merge
                // cli.MergeBase(upstreamBranch, downstreamBranch);
                // cli.ShowRef(upstreamBranch);

                // do the actual merge
                // cli.CheckoutRemote(downstreamBranch);
                // cli.MergeRemote(upstreamBranch);

                // if it was successful
                // cli.Push();

                await detailsTask;
                await latestBranchName;

                var allConfigured = await repository.GetConfiguredBranchGroups().FirstOrDefaultAsync();
                var needsCreate = await repository.GetBranchRef(LatestBranchName).Take(1) == null;
                var upstreamBranches = (await ToUpstreamBranchNames(Details.DirectUpstreamBranchGroups.Select(groupName => allConfigured.Find(g => g.GroupName == groupName)).ToImmutableList())).ToImmutableList();
                var neededUpstreamMerges = needsCreate
                    ? upstreamBranches
                    : await FindNeededMerges(upstreamBranches);

                if (Details.RecreateFromUpstream)
                {
                    if (neededUpstreamMerges.Any())
                    {
                        neededUpstreamMerges = upstreamBranches;

                        needsCreate = true;
                    }

                    if (!needsCreate)
                    {
                        // It's okay if there are still other "upstreamBranches" that didn't get merged, because we already did that check
                        var neededRefs = (await (from branchName in upstreamBranches.ToObservable()
                                                 from branchRef in cli.ShowRef(branchName.BranchName).FirstOutputMessage()
                                                 select branchRef).ToArray()).ToImmutableHashSet();

                        var currentHead = await cli.ShowRef(LatestBranchName).FirstOutputMessage();
                        if (!neededRefs.Contains(currentHead))
                        {
                            // The latest commit of the branch isn't one that we needed. That means it's probably a merge commit!
                            Func<string, IObservable<string[]>> getParents = (commitish) =>
                                cli.GetCommitParents(commitish).FirstOutputMessage().Select(commit => commit.Split(' '));
                            var parents = await getParents(cli.RemoteBranch(LatestBranchName));
                            while (parents.Length > 1)
                            {
                                // Figure out what the other commits are, if any
                                var other = parents.Where(p => !neededRefs.Contains(p)).ToArray();
                                if (other.Length != 1)
                                {
                                    break;
                                }
                                else
                                {
                                    // If there's another commit, it might be a merge of two of our other requireds. Check it!
                                    parents = await getParents(other[0]);
                                }
                            }
                            // If this isn't a merge, then we had a non-merge commit in there; we don't care about the parent.
                            // If it is a merge, we either have 2 "others" or no "others", so looking at one parent will tell us:
                            // If it's a required commit, we're good. If it's not, we need to recreate the branch.
                            needsCreate = parents.Length < 2 || !neededRefs.Contains(parents[0]);
                        }
                    }
                }

                if (needsCreate)
                {
                    await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Out, Message = $"{Details.GroupName} needs to be created from {string.Join(",", neededUpstreamMerges.Select(up => up.GroupName))}" }));
                    await CreateDownstreamBranch(upstreamBranches);
                }
                else if (neededUpstreamMerges.Any())
                {
                    await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Out, Message = $"{LatestBranchName} needs merges from {string.Join(",", neededUpstreamMerges.Select(up => up.GroupName))}" }));

                    await AppendProcess(Queueable(cli.CheckoutRemote(LatestBranchName)));

                    var shouldPush = false;
                    foreach (var upstreamBranch in neededUpstreamMerges)
                    {
                        var result = await MergeUpstreamBranch(upstreamBranch, LatestBranchName);
                        shouldPush = shouldPush || !result.HadConflicts;
                        if (result.HadConflicts)
                        {
                            await AppendProcess(Queueable(cli.Checkout(LatestBranchName)));

                        }
                    }

                    if (shouldPush)
                    {
                        await PushBranch(LatestBranchName);
                    }

                }
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

            private async Task<ImmutableList<NeededMerge>> FindNeededMerges(IEnumerable<NeededMerge> allUpstreamBranches)
            {
                return await (from upstreamBranch in allUpstreamBranches.ToObservable()
                              where upstreamBranch.BranchName != null
                              from hasOutstandingCommit in HasOutstandingCommits(upstreamBranch.BranchName)
                              where hasOutstandingCommit
                              select upstreamBranch)
                    .ToArray()
                    .Select(items => items.ToImmutableList());
            }
            
            private IObservable<bool> HasOutstandingCommits(string upstreamBranch)
            {
                return repository.HasOutstandingCommits(upstreamBranch: upstreamBranch, downstreamBranch: LatestBranchName).Take(1);
            }

            private async Task CreateDownstreamBranch(IEnumerable<NeededMerge> allUpstreamBranches)
            {
                var downstreamBranch = await repository.GetNextCandidateBranch(Details, shouldMutate: true).FirstOrDefaultAsync();
                var validUpstream = allUpstreamBranches.ToImmutableList();
                if (validUpstream.Count == 0)
                {
                    return;
                }

                // Basic process; should have checks on whether or not to create the branch
                var initialBranch = validUpstream[0];

                var checkout = Queueable(cli.CheckoutRemote(initialBranch.BranchName));
                var checkoutError = await AppendProcess(checkout).FirstErrorMessage();
                await checkout;
                if (!string.IsNullOrEmpty(checkoutError) && checkoutError.StartsWith("fatal"))
                {
                    await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{downstreamBranch} unable to be branched from {initialBranch.BranchName}; aborting" }));
                    return;
                }

                await AppendProcess(Queueable(cli.CheckoutNew(downstreamBranch)));

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
                        orchestration.EnqueueAction(new MergeDownstreamAction(downstreamBranchGroup));
#pragma warning restore
                    }
                    return;
                }

                // TODO - create the downstreamBranch based off an earlier commit, then migrate/close PRs, then push actual branch

                await PushBranch(downstreamBranch);

                if (LatestBranchName != null && LatestBranchName != downstreamBranch)
                {
                    await gitServiceApi.MigrateOrClosePullRequests(fromBranch: LatestBranchName, toBranch: downstreamBranch);
                }
            }

            private async Task PushBranch(string downstreamBranch)
            {
                var pushProcess = AppendProcess(Queueable(cli.Push(downstreamBranch)));
                var pushExitCode = await (from o in pushProcess where o.Channel == OutputChannel.ExitCode select o.ExitCode).FirstAsync();
                if (pushExitCode == 0)
                {
                    repository.NotifyPushedRemoteBranch(downstreamBranch);
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
                await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{downstreamBranch} conflicts with {upstreamBranch.GroupName}" }));

                

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

                if (!createdIntegrationBranch.HasValue || createdIntegrationBranch.Value.NeedsPullRequest())
                {
                    if (await gitServiceApi.HasOpenPullRequest(targetBranch: downstreamBranch, sourceBranch: upstreamBranch.BranchName))
                    {
                        return new MergeStatus { HadConflicts = true, Resolution = MergeConflictResolution.PendingPullRequest };
                    }
                    else
                    {
                        // Open a PR if we can't open a new integration branch
                        await PushBranch(downstreamBranch);
                        await gitServiceApi.OpenPullRequest(title: $"Auto-Merge: {downstreamBranch}", targetBranch: downstreamBranch, sourceBranch: upstreamBranch.BranchName, body: "Failed due to merge conflicts.");
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
                    await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{downstreamBranch} had integration branches added." }));
                    return new MergeStatus {
                        HadConflicts = true,
                        Resolution = createdIntegrationBranch.Value.AddedNewIntegrationBranches ? MergeConflictResolution.AddIntegrationBranch : MergeConflictResolution.PendingIntegrationBranch
                    };
                }
            }

            private async Task CleanIndex()
            {
                await AppendProcess(Queueable(cli.Reset()));

                await AppendProcess(Queueable(cli.Clean()));
            }


            private async Task<bool> DoMergeWithCheckout(string upstreamBranch, string targetBranch, string message)
            {
                await AppendProcess(Queueable(cli.CheckoutRemote(targetBranch)));

                return await DoMerge(upstreamBranch, targetBranch, message);
            }


            private async Task<bool> DoMerge(string upstreamBranch, string targetBranch, string message)
            {
                var timestamps = await (from timestampMessage in cli.GetCommitTimestamps(cli.RemoteBranch(upstreamBranch), targetBranch).Output
                                        where timestampMessage.Channel == OutputChannel.Out
                                        select timestampMessage.Message).ToArray();
                var timestamp = timestamps.Max();

                var merge = Queueable(cli.MergeRemote(upstreamBranch, message, commitDate: timestamp));
                var mergeProcess = AppendProcess(merge);
                var mergeExitCode = await (from o in mergeProcess where o.Channel == OutputChannel.ExitCode select o.ExitCode).FirstAsync();
                await mergeProcess;
                var isSuccessfulMerge = mergeExitCode == 0;
                await CleanIndex();
                return isSuccessfulMerge;
            }

            private IObservable<OutputMessage> Queueable(IReactiveProcess reactiveProcess) => reactiveProcess.Output.Replay().ConnectFirst();
        }
    }
}
