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

namespace GitAutomation.Repository.Actions
{
    class MergeDownstreamAction : IRepositoryAction
    {
        private static Regex hasConflict = new Regex("^(<<<<<<<|changed in both)", RegexOptions.Compiled);

        private readonly Subject<OutputMessage> output = new Subject<OutputMessage>();
        private readonly string[] upstreamBranches;
        private readonly string downstreamBranch;

        public string ActionType => "MergeDownstream";

        public MergeDownstreamAction(string[] upstreamBranches, string downstreamBranch)
        {
            this.upstreamBranches = upstreamBranches;
            this.downstreamBranch = downstreamBranch;
        }

        public ImmutableDictionary<string, string> Parameters => new Dictionary<string, string>
            {
                { "upstreamBranches", string.Join(":", upstreamBranches) },
                { "downstreamBranch", downstreamBranch },
            }.ToImmutableDictionary();

        public IObservable<OutputMessage> DeferredOutput => output;

        public IObservable<OutputMessage> PerformAction(IServiceProvider serviceProvider)
        {
            var cli = serviceProvider.GetRequiredService<GitCli>();

            // if these two are different, we need to do the merge
            // cli.MergeBase(upstreamBranch, downstreamBranch);
            // cli.ShowRef(upstreamBranch);

            // check to see if the merge will be successful:
            // git merge-tree {merge-base} {branch-upstream} {branch-downstream}
            // if it contains `^(<<<<<<<\|changed in both)`, it will not be successful, need to recurse and find/create integration branch, which can be done with several merge-base/merge-tree steps.

            // do the actual merge
            // cli.CheckoutRemote(downstreamBranch);
            // cli.MergeRemote(upstreamBranch);
            // cli.Push();

            return Observable.Create<OutputMessage>(async (observer, cancellationToken) =>
            {
                var disposable = new CompositeDisposable();
                var neededUpstreamMerges = new Dictionary<string, string>();
                var processes = new Subject<IObservable<OutputMessage>>();
                disposable.Add(Observable.Concat(processes).Subscribe(observer));
                disposable.Add(processes);

                await System.Threading.Tasks.Task.Yield();

                foreach (var upstreamBranch in upstreamBranches)
                {
                    var mergeBase = Queueable(cli.MergeBase(upstreamBranch, downstreamBranch));
                    var showRef = Queueable(cli.ShowRef(upstreamBranch));

                    processes.OnNext(mergeBase);
                    processes.OnNext(showRef);

                    var mergeBaseResult = await (from o in mergeBase where o.Channel == OutputChannel.Out select o.Message).FirstOrDefaultAsync();
                    var showRefResult = await (from o in showRef where o.Channel == OutputChannel.Out select o.Message).FirstOrDefaultAsync();

                    if (mergeBaseResult != showRefResult)
                    {
                        neededUpstreamMerges.Add(upstreamBranch, mergeBaseResult);
                    }
                }

                if (neededUpstreamMerges.Any())
                {
                    processes.OnNext(Observable.Return(new OutputMessage { Channel = OutputChannel.Out, Message = $"{downstreamBranch} needs merges from {string.Join(",", neededUpstreamMerges.Keys)}" }));

                    // TODO - remaining steps
                    foreach (var upstreamBranch in neededUpstreamMerges.Keys)
                    {
                        var mergeTree = Queueable(cli.MergeTree(neededUpstreamMerges[upstreamBranch], upstreamBranch, downstreamBranch));
                        // Excluding the output because this command is very noisy
                        processes.OnNext(mergeTree.Where(msg => msg.Channel != OutputChannel.Out));

                        var mergeTreeOutput = await (from o in mergeTree where o.Channel == OutputChannel.Out select o.Message).ToList().FirstAsync();
                        if (mergeTreeOutput.Any(hasConflict.IsMatch))
                        {
                            // TODO - conflict!
                            processes.OnNext(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{downstreamBranch} conflicts with {upstreamBranch}" }));
                        }
                        else
                        {
                            var doCleanMerge = Queueable(cli.CheckoutRemote(downstreamBranch))
                                .Concat(Queueable(cli.MergeRemote(upstreamBranch)))
                                .Concat(Queueable(cli.Push(downstreamBranch)));

                            processes.OnNext(doCleanMerge);
                        }
                    }
                }

                processes.OnCompleted();

                return () =>
                {
                    disposable.Dispose();
                };
            });
        }

        private IObservable<OutputMessage> Queueable(IReactiveProcess reactiveProcess) => reactiveProcess.Output.Replay().ConnectFirst();
    }
}
