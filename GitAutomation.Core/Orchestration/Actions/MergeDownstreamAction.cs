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
            PullRequest
        }

        private struct MergeStatus
        {
            public bool HadConflicts;
            public MergeConflictResolution Resolution;
        }

        private readonly Subject<OutputMessage> output = new Subject<OutputMessage>();
        private readonly string downstreamBranch;

        public string ActionType => "MergeDownstream";

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
            private readonly string downstreamBranchGroup;
            private readonly Task<BranchGroupCompleteData> detailsTask;
            private readonly Task<string> latestBranchName;

            private BranchGroupCompleteData Details => detailsTask.Result;
            private string LatestBranchName => latestBranchName.Result;

            public MergeDownstreamActionProcess(GitCli cli, IGitServiceApi gitServiceApi, IUnitOfWorkFactory workFactory, IRepositoryOrchestration orchestration, IRepositoryMediator repository, IntegrateBranchesOrchestration integrateBranches, string downstreamBranch)
            {
                this.cli = cli;
                this.gitServiceApi = gitServiceApi;
                this.integrateBranches = integrateBranches;
                this.repository = repository;
                this.orchestration = orchestration;
                this.downstreamBranchGroup = downstreamBranch;
                this.detailsTask = repository.GetBranchDetails(downstreamBranch).FirstAsync().ToTask();
                this.latestBranchName = detailsTask.ContinueWith(task => repository.LatestBranchName(task.Result).FirstOrDefaultAsync().ToTask()).Unwrap();
            }

            protected override async Task RunProcess()
            {
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
                    : (await FilterUpstreamReadyForMerge(await FindNeededMerges(upstreamBranches), !Details.RecreateFromUpstream));

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
                                                 from branchRef in cli.ShowRef(branchName).FirstOutputMessage()
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
                            // We either have 2 "others" or no "others", so our single parent will tell us.
                            // If it's a required commit, we're good. If it's not, we need to recreate the branch.
                            needsCreate = !neededRefs.Contains(parents[0]);
                        }
                    }
                }

                if (needsCreate)
                {
                    await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Out, Message = $"{Details.GroupName} needs to be created from {string.Join(",", neededUpstreamMerges)}" }));
                    await CreateDownstreamBranch(upstreamBranches);
                }
                else if (neededUpstreamMerges.Any())
                {
                    await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Out, Message = $"{LatestBranchName} needs merges from {string.Join(",", neededUpstreamMerges)}" }));

                    await AppendProcess(Queueable(cli.CheckoutRemote(LatestBranchName)));

                    foreach (var upstreamBranch in neededUpstreamMerges)
                    {
                        await MergeUpstreamBranch(upstreamBranch, LatestBranchName);
                    }

                    await PushBranch(LatestBranchName);

                }
            }

            private async Task<IEnumerable<string>> ToUpstreamBranchNames(ImmutableList<BranchGroupDetails> directUpstreamBranchGroups)
            {
                var hierarchy = (await repository.AllBranchesHierarchy().Take(1)).ToDictionary(branch => branch.GroupName, branch => branch.HierarchyDepth);
                // Put integration branches first; they might be needed for conflict resolution in other branches!
                // FIXME - this is using the GroupName rather than the BranchName!
                return from branch in directUpstreamBranchGroups
                       orderby branch.BranchType == BranchGroupType.Integration ? 0 : 1, -hierarchy[branch.GroupName], branch.GroupName
                       select branch.GroupName;
            }

            private async Task<ImmutableList<string>> FindNeededMerges(IEnumerable<string> allUpstreamBranches)
            {
                return await (from upstreamBranch in allUpstreamBranches.ToObservable()
                              from hasOutstandingCommit in HasOutstandingCommits(upstreamBranch)
                              where hasOutstandingCommit
                              select upstreamBranch)
                    .ToArray()
                    .Select(items => items.ToImmutableList());
            }
            
            private IObservable<bool> HasOutstandingCommits(string upstreamBranch)
            {
                return repository.HasOutstandingCommits(upstreamBranch: upstreamBranch, downstreamBranch: LatestBranchName).Take(1);
            }

            private async Task CreateDownstreamBranch(IEnumerable<string> allUpstreamBranches)
            {
                var downstreamBranch = await repository.GetNextCandidateBranch(Details, shouldMutate: true).FirstOrDefaultAsync();
                var validUpstream = await FilterUpstreamReadyForMerge(allUpstreamBranches, false);

                // Basic process; should have checks on whether or not to create the branch
                var initialBranch = validUpstream.First();

                var checkout = Queueable(cli.CheckoutRemote(initialBranch));
                var checkoutError = await AppendProcess(checkout).FirstErrorMessage();
                await checkout;
                if (!string.IsNullOrEmpty(checkoutError) && checkoutError.StartsWith("fatal"))
                {
                    await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{downstreamBranch} unable to be branched from {initialBranch}; aborting" }));
                    return;
                }

                await AppendProcess(Queueable(cli.CheckoutNew(downstreamBranch)));

                foreach (var upstreamBranch in validUpstream.Skip(1))
                {
                    var result = await MergeUpstreamBranch(upstreamBranch, downstreamBranch);
                    if (result.HadConflicts)
                    {
                        // abort, but queue another attempt
#pragma warning disable CS4014
                        orchestration.EnqueueAction(new MergeDownstreamAction(downstreamBranchGroup));
#pragma warning restore
                        return;
                    }
                }

                await PushBranch(downstreamBranch);

                if (LatestBranchName != null && LatestBranchName != downstreamBranch)
                {
                    await gitServiceApi.MigrateOrClosePullRequests(fromBranch: LatestBranchName, toBranch: downstreamBranch);
                }
            }

            private async Task PushBranch(string downstreamBranch)
            {
                await AppendProcess(Queueable(cli.Push(downstreamBranch)));
                repository.NotifyPushedRemoteBranch(downstreamBranch);
            }

            private async Task<ImmutableList<string>> FilterUpstreamReadyForMerge(IEnumerable<string> allUpstreamBranches, bool checkToTarget)
            {
                var validUpstream = new List<string>();
                foreach (var upstream in allUpstreamBranches)
                {
                    if (!await gitServiceApi.HasOpenPullRequest(targetBranch: upstream) && !(checkToTarget && await gitServiceApi.HasOpenPullRequest(targetBranch: LatestBranchName, sourceBranch: upstream)))
                    {
                        validUpstream.Add(upstream);
                    }
                    else
                    {
                        await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{upstream} skipped due to open pull request" }));
                    }
                }

                return validUpstream.ToImmutableList();
            }

            /// <summary>
            /// Notice - assumes that downstream is already checked out!
            /// </summary>
            private async Task<MergeStatus> MergeUpstreamBranch(string upstreamBranch, string downstreamBranch)
            {
                bool isSuccessfulMerge = await DoMerge(upstreamBranch, downstreamBranch, message: $"Auto-merge branch '{upstreamBranch}'");
                if (isSuccessfulMerge)
                {
                    return new MergeStatus { HadConflicts = false };
                }

                // conflict!
                await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{downstreamBranch} conflicts with {upstreamBranch}" }));

                var canUseIntegrationBranch = Details.BranchType != BranchGroupType.Integration && Details.DirectUpstreamBranchGroups.Count != 1;
                var createdIntegrationBranch = canUseIntegrationBranch
                    ? await integrateBranches.FindAndCreateIntegrationBranches(
                            Details,
                            Details.DirectUpstreamBranchGroups, 
                            DoMergeWithCheckout
                        )
                        .ContinueWith(result => result.Result.AddedNewIntegrationBranches || result.Result.HadPullRequest)
                    : false;

                if (!createdIntegrationBranch)
                {
                    // Open a PR if we can't open a new integration branch
                    await PushBranch(downstreamBranch);
                    await gitServiceApi.OpenPullRequest(title: $"Auto-Merge: {downstreamBranch}", targetBranch: downstreamBranch, sourceBranch: upstreamBranch, body: "Failed due to merge conflicts.");
                    return new MergeStatus { HadConflicts = true, Resolution = MergeConflictResolution.PullRequest };
                }
                else
                {
                    await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{downstreamBranch} had integration branches added." }));
                    return new MergeStatus { HadConflicts = true, Resolution = MergeConflictResolution.AddIntegrationBranch };
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
                var timestamps = await (from timestampMessage in cli.GetCommitTimestamps(cli.RemoteBranch(upstreamBranch), cli.RemoteBranch(targetBranch)).Output
                                        where timestampMessage.Channel == OutputChannel.Out
                                        select timestampMessage.Message).ToArray();
                var timestamp = timestamps.Max();

                var merge = Queueable(cli.MergeRemote(upstreamBranch, message, commitDate: timestamp));
                var mergeExitCode = await (from o in AppendProcess(merge) where o.Channel == OutputChannel.ExitCode select o.ExitCode).FirstAsync();
                var isSuccessfulMerge = mergeExitCode == 0;
                await CleanIndex();
                return isSuccessfulMerge;
            }

            private IObservable<OutputMessage> Queueable(IReactiveProcess reactiveProcess) => reactiveProcess.Output.Replay().ConnectFirst();
        }
    }
}
