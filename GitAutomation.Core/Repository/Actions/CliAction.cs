using GitAutomation.Processes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace GitAutomation.Repository.Actions
{
    abstract class CliAction : IRepositoryAction
    {
        private readonly Subject<OutputMessage> output = new Subject<OutputMessage>();
        
        public abstract string ActionType { get; }

        public virtual ImmutableDictionary<string, string> Parameters =>
            ImmutableDictionary<string, string>.Empty;

        public IObservable<OutputMessage> DeferredOutput => output;

        public virtual IObservable<OutputMessage> PerformAction(IServiceProvider serviceProvider)
        {
            GetCliAction(serviceProvider.GetRequiredService<GitCli>()).Output      
                .Subscribe(output);
            return output;
        }

        protected abstract IReactiveProcess GetCliAction(GitCli gitCli);
    }
}
