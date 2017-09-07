using GitAutomation.BranchSettings;
using GitAutomation.GitService;
using GitAutomation.Processes;
using GitAutomation.Repository;
using GitAutomation.Work;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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

        public ImmutableDictionary<string, string> Parameters => new Dictionary<string, string>
            {
                { "downstreamBranch", downstreamBranch },
            }.ToImmutableDictionary();

        public IObservable<OutputMessage> DeferredOutput => output;

        public IObservable<OutputMessage> PerformAction(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<MergeDownstreamActionProcess>(serviceProvider, downstreamBranch).Process().Multicast(output).RefCount();
        }

        private class MergeDownstreamActionProcess
        {
            private readonly GitCli cli;
            private readonly IBranchSettings settings;
            private readonly IGitServiceApi gitServiceApi;
            private readonly string downstreamBranch;
            private readonly IntegrateBranchesOrchestration integrateBranches;
            private readonly IObservable<OutputMessage> process;
            private readonly Subject<IObservable<OutputMessage>> processes;
            private BranchDetails details;

            public MergeDownstreamActionProcess(GitCli cli, IBranchSettings settings, IGitServiceApi gitServiceApi, IUnitOfWorkFactory workFactory, IRepositoryOrchestration orchestration, IIntegrationNamingMediator integrationNaming, IntegrateBranchesOrchestration integrateBranches, string downstreamBranch)
            {
                this.cli = cli;
                this.settings = settings;
                this.gitServiceApi = gitServiceApi;
                this.downstreamBranch = downstreamBranch;
                this.integrateBranches = integrateBranches;
                var disposable = new CompositeDisposable();
                this.processes = new Subject<IObservable<OutputMessage>>();
                disposable.Add(processes);

                this.process = Observable.Create<OutputMessage>(async (observer, cancellationToken) =>
                {
                    if (disposable.IsDisposed)
                    {
                        observer.OnError(new ObjectDisposedException(nameof(disposable)));
                    }
                    disposable.Add(Observable.Concat(processes).Subscribe(observer));
                    await RunProcess();

                    return () =>
                    {
                        disposable.Dispose();
                    };
                }).Publish().RefCount();
            }

            private async Task RunProcess()
            {
                // if these two are different, we need to do the merge
                // cli.MergeBase(upstreamBranch, downstreamBranch);
                // cli.ShowRef(upstreamBranch);

                // do the actual merge
                // cli.CheckoutRemote(downstreamBranch);
                // cli.MergeRemote(upstreamBranch);

                // if it was successful
                // cli.Push();

                details = await settings.GetBranchDetails(downstreamBranch).FirstAsync();

                var needsRecreate = await cli.ShowRef(downstreamBranch).FirstOutputMessage() == null;
                var neededUpstreamMerges = await FindNeededMerges(details.DirectUpstreamBranches.Select(branch => branch.BranchName));

                if (neededUpstreamMerges.Any() && details.RecreateFromUpstream)
                {
                    neededUpstreamMerges = details.DirectUpstreamBranches.Select(branch => branch.BranchName).ToImmutableList();

                    var deleteRemote = Queueable(cli.DeleteRemote(downstreamBranch));
                    processes.OnNext(deleteRemote);
                    await deleteRemote;

                    needsRecreate = true;
                }

                if (needsRecreate)
                {
                    processes.OnNext(Observable.Return(new OutputMessage { Channel = OutputChannel.Out, Message = $"{downstreamBranch} needs to be created from {string.Join(",", neededUpstreamMerges)}" }));
                    await CreateDownstreamBranch(details.DirectUpstreamBranches.Select(branch => branch.BranchName));
                }
                else if (neededUpstreamMerges.Any())
                {
                    processes.OnNext(Observable.Return(new OutputMessage { Channel = OutputChannel.Out, Message = $"{downstreamBranch} needs merges from {string.Join(",", neededUpstreamMerges)}" }));

                    foreach (var upstreamBranch in neededUpstreamMerges)
                    {
                        await MergeUpstreamBranch(upstreamBranch);
                    }
                }

                processes.OnCompleted();
            }

            public IObservable<OutputMessage> Process()
            {
                return this.process;
            }

            /// <returns>True if the entire branch needs to be created</returns>
            private async Task<ImmutableList<string>> FindNeededMerges(IEnumerable<string> allUpstreamBranches)
            {
                return await (from upstreamBranch in allUpstreamBranches.ToObservable()
                              from hasOutstandingCommit in HasOutstandingCommits(upstreamBranch)
                              where hasOutstandingCommit
                              select upstreamBranch)
                    .ToArray()
                    .Select(items => items.ToImmutableList());
            }

            /// <returns>True if the entire branch needs to be created</returns>
            private IObservable<bool> HasOutstandingCommits(string upstreamBranch)
            {
                return Observable.CombineLatest(
                        cli.MergeBase(upstreamBranch, downstreamBranch).FirstOutputMessage(),
                        cli.ShowRef(upstreamBranch).FirstOutputMessage(),
                        (mergeBaseResult, showRefResult) => mergeBaseResult != showRefResult
                    );
            }

            private async Task CreateDownstreamBranch(IEnumerable<string> allUpstreamBranches)
            {
                var validUpstream = await FilterUpstreamReadyForMerge(allUpstreamBranches);

                // Basic process; should have checks on whether or not to create the branch
                var initialBranch = validUpstream.First();

                var checkout = Queueable(cli.CheckoutRemote(initialBranch));
                processes.OnNext(checkout);
                var checkoutError = await checkout.FirstErrorMessage();
                await checkout;
                if (!string.IsNullOrEmpty(checkoutError) && checkoutError.StartsWith("fatal"))
                {
                    processes.OnNext(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{downstreamBranch} unable to be branched from {initialBranch}; aborting" }));
                    return;
                }

                var createBranch = Queueable(cli.CheckoutNew(downstreamBranch));
                processes.OnNext(createBranch);
                await createBranch;

                var push = Queueable(cli.Push(downstreamBranch));
                processes.OnNext(push);
                await push;

                foreach (var upstreamBranch in validUpstream.Skip(1))
                {
                    await MergeUpstreamBranch(upstreamBranch);
                }
            }

            private async Task<List<string>> FilterUpstreamReadyForMerge(IEnumerable<string> allUpstreamBranches)
            {
                var validUpstream = new List<string>();
                foreach (var upstream in allUpstreamBranches)
                {
                    if (!await gitServiceApi.HasOpenPullRequest(targetBranch: upstream) && !await gitServiceApi.HasOpenPullRequest(targetBranch: downstreamBranch, sourceBranch: upstream))
                    {
                        validUpstream.Add(upstream);
                    }
                    else
                    {
                        processes.OnNext(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{upstream} skipped due to open pull request" }));
                    }
                }

                return validUpstream;
            }

            /// <summary>
            /// Notice - assumes that downstream is already checked out!
            /// </summary>
            private async Task MergeUpstreamBranch(string upstreamBranch)
            {
                bool isSuccessfulMerge = await DoMerge(upstreamBranch, downstreamBranch, message: $"Auto-merge branch '{upstreamBranch}' into '{downstreamBranch}'");
                if (isSuccessfulMerge)
                {
                    var push = Queueable(cli.Push(downstreamBranch, downstreamBranch));
                    processes.OnNext(push);
                    await push;
                }
                else
                {
                    // conflict!
                    processes.OnNext(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{downstreamBranch} conflicts with {upstreamBranch}" }));

                    var canUseIntegrationBranch = details.BranchType != BranchType.Integration && details.DirectUpstreamBranches.Count != 1;
                    var createdIntegrationBranch = canUseIntegrationBranch
                        ? await integrateBranches.FindAndCreateIntegrationBranches(
                                details, 
                                details.DirectUpstreamBranches.Select(branch => branch.BranchName), 
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
                        processes.OnNext(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{downstreamBranch} had integration branches added." }));
                    }
                }
            }

            private async Task CleanIndex()
            {
                var reset = Queueable(cli.Reset());
                processes.OnNext(reset);
                await reset;

                var clean = Queueable(cli.Clean());
                processes.OnNext(clean);
                await clean;
            }

            private async Task<bool> DoMerge(string upstreamBranch, string targetBranch, string message)
            {
                var checkout = Queueable(cli.CheckoutRemote(targetBranch));
                processes.OnNext(checkout);
                await checkout;

                var merge = Queueable(cli.MergeRemote(upstreamBranch, message));
                processes.OnNext(merge);
                var mergeExitCode = await (from o in merge where o.Channel == OutputChannel.ExitCode select o.ExitCode).FirstAsync();
                var isSuccessfulMerge = mergeExitCode == 0;
                await CleanIndex();
                return isSuccessfulMerge;
            }

            private IObservable<OutputMessage> Queueable(IReactiveProcess reactiveProcess) => reactiveProcess.Output.Replay().ConnectFirst();
        }
    }
}
