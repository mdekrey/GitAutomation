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
            private readonly IBranchSettings settings;
            private readonly IGitServiceApi gitServiceApi;
            private readonly IntegrateBranchesOrchestration integrateBranches;
            private readonly IRepositoryMediator repository;
            private readonly Task<BranchDetails> detailsTask;
            private readonly Task<string> latestBranchName;

            private BranchDetails Details => detailsTask.Result;
            private string LatestBranchName => latestBranchName.Result;

            public MergeDownstreamActionProcess(GitCli cli, IBranchSettings settings, IGitServiceApi gitServiceApi, IUnitOfWorkFactory workFactory, IRepositoryOrchestration orchestration, IRepositoryMediator repository, IntegrateBranchesOrchestration integrateBranches, string downstreamBranch)
            {
                this.cli = cli;
                this.settings = settings;
                this.gitServiceApi = gitServiceApi;
                this.integrateBranches = integrateBranches;
                this.repository = repository;
                this.detailsTask = settings.GetBranchDetails(downstreamBranch).FirstAsync().ToTask();
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

                // TODO - latest downstreamBranch might not be named the same as the value of downstreamBranch due to 
                // preserving previous branches. Need to check with repository to determine this.
                await detailsTask;
                await latestBranchName;

                var needsCreate = await cli.ShowRef(LatestBranchName).FirstOutputMessage() == null;
                var neededUpstreamMerges = needsCreate
                    ? Details.DirectUpstreamBranches.Select(branch => branch.BranchName).ToImmutableList()
                    : (await FilterUpstreamReadyForMerge(await FindNeededMerges(Details.DirectUpstreamBranches.Select(branch => branch.BranchName)))).ToImmutableList();

                if (neededUpstreamMerges.Any() && Details.RecreateFromUpstream)
                {
                    neededUpstreamMerges = Details.DirectUpstreamBranches.Select(branch => branch.BranchName).ToImmutableList();
                    
                    needsCreate = true;
                }

                if (needsCreate)
                {
                    await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Out, Message = $"{Details.BranchName} needs to be created from {string.Join(",", neededUpstreamMerges)}" }));
                    await CreateDownstreamBranch(Details.DirectUpstreamBranches.Select(branch => branch.BranchName));
                }
                else if (neededUpstreamMerges.Any())
                {
                    await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Out, Message = $"{LatestBranchName} needs merges from {string.Join(",", neededUpstreamMerges)}" }));

                    foreach (var upstreamBranch in neededUpstreamMerges)
                    {
                        await MergeUpstreamBranch(upstreamBranch, LatestBranchName);
                    }
                }
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
                return cli.HasOutstandingCommits(upstreamBranch: upstreamBranch, downstreamBranch: LatestBranchName);
            }

            private async Task CreateDownstreamBranch(IEnumerable<string> allUpstreamBranches)
            {
                var downstreamBranch = await repository.GetNextCandidateBranch(Details, shouldMutate: true).FirstOrDefaultAsync();
                var validUpstream = await FilterUpstreamReadyForMerge(allUpstreamBranches);

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

                await AppendProcess(Queueable(cli.Push(downstreamBranch)));

                foreach (var upstreamBranch in validUpstream.Skip(1))
                {
                    await MergeUpstreamBranch(upstreamBranch, downstreamBranch);
                }
            }

            private async Task<List<string>> FilterUpstreamReadyForMerge(IEnumerable<string> allUpstreamBranches)
            {
                var validUpstream = new List<string>();
                foreach (var upstream in allUpstreamBranches)
                {
                    if (!await gitServiceApi.HasOpenPullRequest(targetBranch: upstream) && !await gitServiceApi.HasOpenPullRequest(targetBranch: LatestBranchName, sourceBranch: upstream))
                    {
                        validUpstream.Add(upstream);
                    }
                    else
                    {
                        await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{upstream} skipped due to open pull request" }));
                    }
                }

                return validUpstream;
            }

            /// <summary>
            /// Notice - assumes that downstream is already checked out!
            /// </summary>
            private async Task MergeUpstreamBranch(string upstreamBranch, string downstreamBranch)
            {
                bool isSuccessfulMerge = await DoMerge(upstreamBranch, downstreamBranch, message: $"Auto-merge branch '{upstreamBranch}'");
                if (isSuccessfulMerge)
                {
                    await AppendProcess(Queueable(cli.Push(downstreamBranch, downstreamBranch)));
                }
                else
                {
                    // conflict!
                    await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{downstreamBranch} conflicts with {upstreamBranch}" }));

                    var canUseIntegrationBranch = Details.BranchType != BranchType.Integration && Details.DirectUpstreamBranches.Count != 1;
                    var createdIntegrationBranch = canUseIntegrationBranch
                        ? await integrateBranches.FindAndCreateIntegrationBranches(
                                Details,
                                Details.DirectUpstreamBranches.Select(branch => branch.BranchName), 
                                DoMerge
                            )
                            .ContinueWith(result => result.Result.AddedNewIntegrationBranches || result.Result.HadPullRequest)
                        : false;

                    if (!createdIntegrationBranch)
                    {
                        // Open a PR if we can't open a new integration branch
                        await gitServiceApi.OpenPullRequest(title: $"Auto-Merge: {downstreamBranch}", targetBranch: downstreamBranch, sourceBranch: upstreamBranch, body: "Failed due to merge conflicts.");
                    }
                    else
                    {
                        await AppendProcess(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{downstreamBranch} had integration branches added." }));
                    }
                }
            }

            private async Task CleanIndex()
            {
                await AppendProcess(Queueable(cli.Reset()));

                await AppendProcess(Queueable(cli.Clean()));
            }

            private async Task<bool> DoMerge(string upstreamBranch, string targetBranch, string message)
            {
                await AppendProcess(Queueable(cli.CheckoutRemote(targetBranch)));

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
