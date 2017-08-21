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
    class DeleteBranchAction : IRepositoryAction
    {
        private readonly Subject<OutputMessage> output = new Subject<OutputMessage>();
        private readonly string deletingBranch;

        public string ActionType => "ConsolidateServiceLine";

        public DeleteBranchAction(string deletingBranch)
        {
            this.deletingBranch = deletingBranch;
        }

        public ImmutableDictionary<string, string> Parameters => new Dictionary<string, string>
            {
                { "deletingBranch", deletingBranch },
            }.ToImmutableDictionary();

        public IObservable<OutputMessage> DeferredOutput => output;

        public IObservable<OutputMessage> PerformAction(IServiceProvider serviceProvider)
        {
            var cli = serviceProvider.GetRequiredService<GitCli>();
            var settings = serviceProvider.GetRequiredService<IBranchSettings>();
            var unitOfWorkFactory = serviceProvider.GetRequiredService<IUnitOfWorkFactory>();
            
            return Observable.Create<OutputMessage>(async (observer, cancellationToken) =>
            {
                var disposable = new CompositeDisposable();
                var processes = new Subject<IObservable<OutputMessage>>();
                disposable.Add(Observable.Concat(processes).Subscribe(observer));
                disposable.Add(processes);

                using (var unitOfWork = unitOfWorkFactory.CreateUnitOfWork())
                {
                    settings.DeleteBranchSettings(deletingBranch, unitOfWork);

                    await unitOfWork.CommitAsync();
                }

                var deleteBranch = Queueable(cli.DeleteRemote(deletingBranch));
                processes.OnNext(deleteBranch);
                await deleteBranch;
                
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
        
        private IObservable<OutputMessage> Queueable(IReactiveProcess reactiveProcess) => reactiveProcess.Output.Replay().ConnectFirst();
    }
}
