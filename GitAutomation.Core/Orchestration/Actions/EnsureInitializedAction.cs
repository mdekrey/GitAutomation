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
using GitAutomation.Repository;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Orchestration.Actions
{
    class EnsureInitializedAction : ComplexAction<EnsureInitializedAction.Internal>
    {
        public override string ActionType => "EnsureInitialized";

        public override JToken Parameters => JToken.FromObject(ImmutableDictionary<string, string>.Empty);

        public class Internal : ComplexActionInternal
        {
            private readonly IGitCli cli;
            private readonly GitRepositoryOptions gitOptions;

            public Internal(IGitCli cli, IOptions<GitRepositoryOptions> options)
            {
                this.cli = cli;
                this.gitOptions = options.Value;
            }

            protected override async Task RunProcess()
            {
                if (cli.IsGitInitialized)
                {
                    return;
                }

                var checkoutPath = gitOptions.CheckoutPath;

                if (!Directory.Exists(checkoutPath))
                {
                    Directory.CreateDirectory(checkoutPath);
                }

                await AppendProcess(cli.Clone()).WaitUntilComplete();
                await AppendProcess(cli.Config("user.name", gitOptions.UserName)).WaitUntilComplete();
                await AppendProcess(cli.Config("user.email", gitOptions.UserEmail)).WaitUntilComplete();
            }
        }
    }
}
