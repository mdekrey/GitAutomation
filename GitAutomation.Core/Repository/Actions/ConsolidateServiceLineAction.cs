using GitAutomation.BranchSettings;
using GitAutomation.Processes;
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
namespace GitAutomation.Repository.Actions
{
    class ConsolidateServiceLineAction : IRepositoryAction
    {
        private readonly Subject<OutputMessage> output = new Subject<OutputMessage>();
        private readonly string releaseCandidateBranch;
        private readonly string serviceLineBranch;
        private readonly string tagName;

        public string ActionType => "ConsolidateServiceLine";

        public ConsolidateServiceLineAction(string releaseCandidateBranch, string serviceLineBranch, string tagName)
        {
            this.releaseCandidateBranch = releaseCandidateBranch;
            this.serviceLineBranch = serviceLineBranch;
            this.tagName = tagName;
        }

        public ImmutableDictionary<string, string> Parameters => new Dictionary<string, string>
            {
                { "releaseCandidateBranch", releaseCandidateBranch },
                { "serviceLineBranch", serviceLineBranch },
            }.ToImmutableDictionary();

        public IObservable<OutputMessage> DeferredOutput => output;

        public IObservable<OutputMessage> PerformAction(IServiceProvider serviceProvider)
        {
            var cli = serviceProvider.GetRequiredService<GitCli>();
            var settings = serviceProvider.GetRequiredService<IBranchSettings>();
            var unitOfWorkFactory = serviceProvider.GetRequiredService<IUnitOfWorkFactory>();

            // either:
            // 1. create new service line from release candidate
            // 2. merge --ff-only from release candidate to service line

            // if it passes:
            //   collect upstream branches
            //   push service line
            //   run consolidate service line SQL
            //   delete old upstream branches and release candidate

            return Observable.Create<OutputMessage>(async (observer, cancellationToken) =>
            {
                var disposable = new CompositeDisposable();
                var processes = new Subject<IObservable<OutputMessage>>();
                disposable.Add(Observable.Concat(processes).Subscribe(observer));
                disposable.Add(processes);

                var tag = Queueable(cli.Tag(tagName, $"Automated release to service line {serviceLineBranch} from {releaseCandidateBranch}"));
                processes.OnNext(tag);
                await tag;

                var pushTag = Queueable(cli.Push(tagName));
                processes.OnNext(pushTag);
                await pushTag;

                var readyToFinalize = await CreateOrFastForwardServiceLine(cli, processes);

                if (!readyToFinalize)
                {
                    processes.OnNext(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{serviceLineBranch} unable to be fast-forwarded from {releaseCandidateBranch}; aborting" }));
                }
                else
                {
                    var branchesToRemove = await settings.GetAllUpstreamRemovableBranches(releaseCandidateBranch).FirstAsync();

                    var push = Queueable(cli.Push(serviceLineBranch));
                    processes.OnNext(push);
                    await push;

                    using (var unitOfWork = unitOfWorkFactory.CreateUnitOfWork())
                    {
                        settings.ConsolidateServiceLine(releaseCandidateBranch, serviceLineBranch, unitOfWork);

                        await unitOfWork.CommitAsync();
                    }

                    foreach (var branch in branchesToRemove.Concat(new[] { releaseCandidateBranch }))
                    {
                        var deleteBranch = Queueable(cli.DeleteRemote(branch));
                        processes.OnNext(deleteBranch);
                        await deleteBranch;
                    }

                }

                var fetch = Queueable(cli.Fetch());
                processes.OnNext(fetch);
                await fetch;

                processes.OnCompleted();

                return () =>
                {
                    disposable.Dispose();
                };
            }).Multicast(output).RefCount();
        }

        private async Task<bool> CreateOrFastForwardServiceLine(GitCli cli, Subject<IObservable<OutputMessage>> processes)
        {
            var showRef = Queueable(cli.ShowRef(serviceLineBranch));
            processes.OnNext(showRef);
            var showRefResult = await (from o in showRef where o.Channel == OutputChannel.Out select o.Message).FirstOrDefaultAsync();
            if (showRefResult == null)
            {
                // create service line
                var checkout = Queueable(cli.CheckoutRemote(releaseCandidateBranch));
                processes.OnNext(checkout);
                await checkout;

                var createServiceLine = Queueable(cli.CheckoutNew(serviceLineBranch));
                processes.OnNext(createServiceLine);
                await createServiceLine;

                return true;
            }
            else
            {
                // fast-forward
                var checkout = Queueable(cli.CheckoutRemote(serviceLineBranch));
                processes.OnNext(checkout);
                await checkout;

                var createServiceLine = Queueable(cli.MergeFastForward(releaseCandidateBranch));
                processes.OnNext(createServiceLine);
                var fastForwardResult = await (from o in showRef where o.Channel == OutputChannel.ExitCode select o.ExitCode).FirstOrDefaultAsync();

                return fastForwardResult == 0;
            }
        }

        private IObservable<OutputMessage> Queueable(IReactiveProcess reactiveProcess) => reactiveProcess.Output.Replay().ConnectFirst();
    }
}
