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
    class EnsureInitializedAction : CliAction
    {
        public override string ActionType => "EnsureInitialized";

        public override IObservable<OutputMessage> PerformAction(IServiceProvider serviceProvider)
        {
            if (serviceProvider.GetRequiredService<GitCli>().IsGitInitialized)
            {
                return Observable.Empty<OutputMessage>();
            }

            var checkoutPath = serviceProvider.GetRequiredService<IOptions<GitRepositoryOptions>>().Value.CheckoutPath;

            if (!Directory.Exists(checkoutPath))
            {
                Directory.CreateDirectory(checkoutPath);
            }

            return base.PerformAction(serviceProvider);
        }

        protected override IReactiveProcess GetCliAction(GitCli gitCli) => gitCli.Clone();
    }
}
