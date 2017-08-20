using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using GitAutomation.Processes;
using Microsoft.Extensions.DependencyInjection;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using Microsoft.Extensions.Options;
using System.IO;

namespace GitAutomation.Repository.Actions
{
    class EnsureInitializedAction : IRepositoryAction
    {
        public string ActionType => "EnsureInitialized";

        private readonly Subject<OutputMessage> output = new Subject<OutputMessage>();

        public ImmutableDictionary<string, string> Parameters => ImmutableDictionary<string, string>.Empty;

        public IObservable<OutputMessage> DeferredOutput => output;

        public IObservable<OutputMessage> PerformAction(IServiceProvider serviceProvider)
        {
            var cli = serviceProvider.GetRequiredService<GitCli>();
            var gitOptions = serviceProvider.GetRequiredService<IOptions<GitRepositoryOptions>>().Value;

            if (cli.IsGitInitialized)
            {
                return Observable.Empty<OutputMessage>();
            }

            var checkoutPath = gitOptions.CheckoutPath;

            if (!Directory.Exists(checkoutPath))
            {
                Directory.CreateDirectory(checkoutPath);
            }

            return Queueable(cli.Clone())
                .Concat(Queueable(cli.Config("user.name", gitOptions.UserName)))
                .Concat(Queueable(cli.Config("user.email", gitOptions.UserEmail)));
        }

        private IObservable<OutputMessage> Queueable(IReactiveProcess reactiveProcess) => reactiveProcess.Output.Replay().ConnectFirst();

    }
}
