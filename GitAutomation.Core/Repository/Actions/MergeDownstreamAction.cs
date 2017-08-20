using GitAutomation.BranchSettings;
using GitAutomation.Processes;
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

namespace GitAutomation.Repository.Actions
{
    class MergeDownstreamAction : IRepositoryAction
    {
        private static Regex hasConflict = new Regex("^(<<<<<<<|changed in both)", RegexOptions.Compiled);

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
            var cli = serviceProvider.GetRequiredService<GitCli>();
            var settings = serviceProvider.GetRequiredService<IBranchSettings>();

            // if these two are different, we need to do the merge
            // cli.MergeBase(upstreamBranch, downstreamBranch);
            // cli.ShowRef(upstreamBranch);
            
            // do the actual merge
            // cli.CheckoutRemote(downstreamBranch);
            // cli.MergeRemote(upstreamBranch);

            // if it was successful
            // cli.Push();

            return Observable.Create<OutputMessage>(async (observer, cancellationToken) =>
            {
                var details = await settings.GetBranchDetails(this.downstreamBranch).FirstAsync();
                var disposable = new CompositeDisposable();
                var neededUpstreamMerges = new HashSet<string>();
                var processes = new Subject<IObservable<OutputMessage>>();
                disposable.Add(Observable.Concat(processes).Subscribe(observer));
                disposable.Add(processes);

                await System.Threading.Tasks.Task.Yield();
                var needsRecreate = await FindNeededMerges(details.DirectUpstreamBranches, neededUpstreamMerges, cli, processes);

                if (neededUpstreamMerges.Any() && details.RecreateFromUpstream)
                {
                    var deleteRemote = Queueable(cli.DeleteRemote(downstreamBranch));
                    processes.OnNext(deleteRemote);
                    await deleteRemote;

                    needsRecreate = true;
                }

                if (needsRecreate)
                {
                    processes.OnNext(Observable.Return(new OutputMessage { Channel = OutputChannel.Out, Message = $"{downstreamBranch} needs to be created from {string.Join(",", neededUpstreamMerges)}" }));
                    await CreateDownstreamBranch(details.DirectUpstreamBranches, cli, processes);
                }
                else if (neededUpstreamMerges.Any())
                {
                    processes.OnNext(Observable.Return(new OutputMessage { Channel = OutputChannel.Out, Message = $"{downstreamBranch} needs merges from {string.Join(",", neededUpstreamMerges)}" }));

                    var checkout = Queueable(cli.CheckoutRemote(downstreamBranch));
                    processes.OnNext(checkout);
                    await checkout;

                    foreach (var upstreamBranch in neededUpstreamMerges)
                    {
                        await MergeUpstreamBranch(upstreamBranch, cli, processes);
                    }
                }

                processes.OnCompleted();

                return () =>
                {
                    disposable.Dispose();
                };
            }).Multicast(output).RefCount();
        }

        /// <returns>True if the entire branch needs to be created</returns>
        private async Task<bool> FindNeededMerges(ImmutableList<string> allUpstreamBranches, HashSet<string> neededUpstreamMerges, GitCli cli, Subject<IObservable<OutputMessage>> processes)
        {
            foreach (var upstreamBranch in allUpstreamBranches)
            {
                var mergeBase = Queueable(cli.MergeBase(upstreamBranch, downstreamBranch));
                var showRef = Queueable(cli.ShowRef(upstreamBranch));

                processes.OnNext(mergeBase);
                var mergeBaseResult = await (from o in mergeBase where o.Channel == OutputChannel.Out select o.Message).FirstOrDefaultAsync();
                if (mergeBaseResult == null)
                {
                    return true;
                }

                processes.OnNext(showRef);
                var showRefResult = await (from o in showRef where o.Channel == OutputChannel.Out select o.Message).FirstOrDefaultAsync();

                if (mergeBaseResult != showRefResult)
                {
                    neededUpstreamMerges.Add(upstreamBranch);
                }
            }

            return false;
        }

        private async Task CreateDownstreamBranch(ImmutableList<string> allUpstreamBranches, GitCli cli, Subject<IObservable<OutputMessage>> processes)
        {
            // Basic process; should have checks on whether or not to create the branch
            var initialBranch = allUpstreamBranches.First();

            var checkout = Queueable(cli.CheckoutRemote(initialBranch));
            processes.OnNext(checkout);
            var checkoutError = await (from o in checkout where o.Channel == OutputChannel.Error select o.Message).FirstOrDefaultAsync();
            await checkout;
            if (!string.IsNullOrEmpty(checkoutError) && checkoutError.StartsWith("fatal"))
            {
                processes.OnNext(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{downstreamBranch} unable to be branched from {initialBranch}; aborting" }));
                return;
            }

            var createBranch = Queueable(cli.CheckoutNew(downstreamBranch));
            processes.OnNext(createBranch);
            await createBranch;

            foreach (var upstreamBranch in allUpstreamBranches.Skip(1))
            {
                await MergeUpstreamBranch(upstreamBranch, cli, processes);
            }
        }

        /// <summary>
        /// Notice - assumes that downstream is already checked out!
        /// </summary>
        private async Task MergeUpstreamBranch(string upstreamBranch, GitCli cli, Subject<IObservable<OutputMessage>> processes)
        {
            var merge = Queueable(cli.MergeRemote(upstreamBranch, message: $"Auto-merge branch '{upstreamBranch}' into '{downstreamBranch}'"));
            processes.OnNext(merge);
            var mergeExitCode = await (from o in merge where o.Channel == OutputChannel.ExitCode select o.ExitCode).FirstAsync();
            if (mergeExitCode == 0)
            {
                processes.OnNext(Queueable(cli.Push(downstreamBranch, downstreamBranch)));
            }
            else
            {
                // TODO - conflict!
                processes.OnNext(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{downstreamBranch} conflicts with {upstreamBranch}" }));

                var reset = Queueable(cli.Reset());
                processes.OnNext(reset);
                await reset;
            }
        }

        private IObservable<OutputMessage> Queueable(IReactiveProcess reactiveProcess) => reactiveProcess.Output.Replay().ConnectFirst();
    }
}
