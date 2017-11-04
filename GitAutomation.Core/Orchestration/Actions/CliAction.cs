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
        private readonly Subject<OutputMessage> output = new Subject<OutputMessage>();
        private readonly ISubject<IObservable<OutputMessage>> outputStream;

        public CliAction()
        {
            outputStream = new BehaviorSubject<IObservable<OutputMessage>>(output);
        }

        public abstract string ActionType { get; }

        public virtual JToken Parameters => JToken.FromObject(
            ImmutableDictionary<string, string>.Empty
        );

        public IObservable<OutputMessage> DeferredOutput => outputStream.Switch();

        public virtual IObservable<OutputMessage> PerformAction(IServiceProvider serviceProvider)
        {
            return GetCliAction(serviceProvider.GetRequiredService<GitCli>()).Output
                .Multicast(output).ConnectFirst();
        }

        protected abstract IReactiveProcess GetCliAction(GitCli gitCli);

        protected void Abort(IObservable<OutputMessage> alternate)
        {
            outputStream.OnNext(alternate);
        }
    }
}
