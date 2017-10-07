using GitAutomation.BranchSettings;
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
    class DeleteBranchAction : IRepositoryAction
    {
        private readonly Subject<OutputMessage> output = new Subject<OutputMessage>();
        private readonly string deletingBranch;

        public string ActionType => "DeleteBranch";

        public DeleteBranchAction(string deletingBranch)
        {
            this.deletingBranch = deletingBranch;
        }

        public JToken Parameters => JToken.FromObject(new Dictionary<string, string>
            {
                { "deletingBranch", deletingBranch },
            }.ToImmutableDictionary());

        public IObservable<OutputMessage> DeferredOutput => output;

        public IObservable<OutputMessage> PerformAction(IServiceProvider serviceProvider)
        {
            var cli = serviceProvider.GetRequiredService<GitCli>();
            var settings = serviceProvider.GetRequiredService<IBranchSettings>();
            var repository = serviceProvider.GetRequiredService<IRepositoryMediator>();
            var unitOfWorkFactory = serviceProvider.GetRequiredService<IUnitOfWorkFactory>();
            
            return Observable.Create<OutputMessage>(async (observer, cancellationToken) =>
            {
                var disposable = new CompositeDisposable();
                var processes = new Subject<IObservable<OutputMessage>>();
                disposable.Add(Observable.Concat(processes).Subscribe(observer));
                disposable.Add(processes);

                var details = await repository.GetBranchDetails(deletingBranch).FirstAsync();

                using (var unitOfWork = unitOfWorkFactory.CreateUnitOfWork())
                {
                    settings.DeleteBranchSettings(deletingBranch, unitOfWork);

                    await unitOfWork.CommitAsync();
                }

                foreach (var branch in details.Branches)
                {
                    var deleteBranch = Queueable(cli.DeleteRemote(branch.Name));
                    processes.OnNext(deleteBranch);
                    await deleteBranch;
                    repository.NotifyPushedRemoteBranch(branch.Name);
                }


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
