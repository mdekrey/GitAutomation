﻿using GitAutomation.BranchSettings;
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace GitAutomation.Orchestration.Actions
{
    class ReleaseToServiceLineAction : IRepositoryAction
    {
        private readonly Subject<OutputMessage> output = new Subject<OutputMessage>();
        private readonly string releaseCandidateBranch;
        private readonly string serviceLineBranch;
        private readonly string tagName;

        public string ActionType => "ReleaseToServiceLine";

        public ReleaseToServiceLineAction(string releaseCandidateBranch, string serviceLineBranch, string tagName)
        {
            this.releaseCandidateBranch = releaseCandidateBranch;
            this.serviceLineBranch = serviceLineBranch;
            this.tagName = tagName;
        }

        public JToken Parameters => JToken.FromObject(new Dictionary<string, string>
            {
                { "releaseCandidateBranch", releaseCandidateBranch },
                { "serviceLineBranch", serviceLineBranch },
            }.ToImmutableDictionary());

        public IObservable<OutputMessage> DeferredOutput => output;

        public IObservable<OutputMessage> PerformAction(IServiceProvider serviceProvider)
        {
            var cli = serviceProvider.GetRequiredService<GitCli>();
            var repository = serviceProvider.GetRequiredService<IRepositoryMediator>();
            var settings = serviceProvider.GetRequiredService<IBranchSettings>();
            var unitOfWorkFactory = serviceProvider.GetRequiredService<IUnitOfWorkFactory>();

            // either:
            // 1. create new service line from release candidate
            // 2. merge --ff-only from release candidate to service line

            // if it passes:
            //   collect upstream branches
            //   push service line

            return Observable.Create<OutputMessage>(async (observer, cancellationToken) =>
            {
                var details = await repository.GetBranchDetails(releaseCandidateBranch).FirstOrDefaultAsync();
                var latestBranchName = await repository.LatestBranchName(details).FirstOrDefaultAsync();
                var disposable = new CompositeDisposable();
                var processes = new Subject<IObservable<OutputMessage>>();
                disposable.Add(Observable.Concat(processes).Subscribe(observer));
                disposable.Add(processes);

                var readyToFinalize = await CreateOrFastForwardServiceLine(latestBranchName, repository, cli, processes);

                if (!readyToFinalize)
                {
                    processes.OnNext(Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"{serviceLineBranch} unable to be fast-forwarded from {latestBranchName}; aborting" }));
                }
                else
                {
                    var tag = Queueable(cli.AnnotatedTag(tagName, $"Automated release to service line {serviceLineBranch} from {latestBranchName}"));
                    processes.OnNext(tag);
                    await tag;

                    var pushTag = Queueable(cli.Push(tagName));
                    processes.OnNext(pushTag);
                    await pushTag;
                    
                    var push = Queueable(cli.Push(serviceLineBranch));
                    processes.OnNext(push);
                    await push;
                }

                processes.OnCompleted();

                return () =>
                {
                    disposable.Dispose();
                };
            }).Multicast(output).RefCount();
        }

        private async Task<bool> CreateOrFastForwardServiceLine(string latestBranchName, IRepositoryMediator repository, GitCli cli, Subject<IObservable<OutputMessage>> processes)
        {
            var showRefResult = await repository.GetBranchRef(serviceLineBranch).Take(1);
            if (showRefResult == null)
            {
                // create service line
                var checkout = Queueable(cli.CheckoutRemote(latestBranchName));
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

                var createServiceLine = Queueable(cli.MergeFastForward(latestBranchName));
                processes.OnNext(createServiceLine);
                var fastForwardResult = await (from o in createServiceLine where o.Channel == OutputChannel.ExitCode select o.ExitCode).FirstOrDefaultAsync();

                return fastForwardResult == 0;
            }
        }

        private IObservable<OutputMessage> Queueable(IReactiveProcess reactiveProcess) => reactiveProcess.Output.Replay().ConnectFirst();
    }
}