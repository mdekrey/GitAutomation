using GitAutomation.Processes;
using GitAutomation.Repository;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace GitAutomation.Orchestration.Actions
{
    abstract class CliAction : IRepositoryAction
    {
        private readonly Subject<IRepositoryActionEntry> output = new Subject<IRepositoryActionEntry>();

        public CliAction()
        {
        }

        public abstract string ActionType { get; }

        public virtual JToken Parameters => JToken.FromObject(
            ImmutableDictionary<string, string>.Empty
        );

        public IObservable<IRepositoryActionEntry> ProcessStream => output;

        public virtual IObservable<IRepositoryActionEntry> PerformAction(IServiceProvider serviceProvider)
        {
            return Observable.Return(new RepositoryActionReactiveProcessEntry(GetCliAction(serviceProvider.GetRequiredService<IGitCli>())))
                .Multicast(output).ConnectFirst();
        }

        protected abstract IReactiveProcess GetCliAction(IGitCli gitCli);

        protected void Abort(IObservable<IRepositoryActionEntry> alternate)
        {
            alternate.Multicast(output).Connect();
        }
    }
}
