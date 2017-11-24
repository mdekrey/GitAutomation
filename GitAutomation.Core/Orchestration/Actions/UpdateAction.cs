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
using System.Threading.Tasks;

namespace GitAutomation.Orchestration.Actions
{
    class UpdateAction : ComplexUniqueAction<UpdateAction.Internal>
    {
        public override string ActionType => "Update";

        public override JToken Parameters => JToken.FromObject(ImmutableDictionary<string, string>.Empty);

        public class Internal : ComplexActionInternal
        {
            private readonly GitCli cli;
            private readonly IRepositoryState repositoryState;

            public Internal(GitCli cli, IRepositoryState repositoryState)
            {
                this.cli = cli;
                this.repositoryState = repositoryState;
            }
            
            protected override async Task RunProcess()
            {
                await AppendProcess(cli.IsGitInitialized
                    ? cli.Fetch()
                    : cli.Clone());

                repositoryState.NotifyUpdated();
            }
        }
    }
}
