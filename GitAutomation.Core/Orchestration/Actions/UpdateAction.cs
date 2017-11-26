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
        private readonly string branchName;

        public override string ActionType => "Update";

        public override JToken Parameters => branchName == null
            ? JToken.FromObject(ImmutableDictionary<string, string>.Empty)
            : JToken.FromObject(new { branchName });

        public UpdateAction(string branchName = null)
        {
            this.branchName = branchName;
        }

        internal override object[] GetExtraParameters()
        {
            return branchName == null
                ? base.GetExtraParameters()
                : new object[] { branchName };
        }

        public class Internal : ComplexActionInternal
        {
            private readonly IGitCli cli;
            private readonly IRemoteRepositoryState repositoryState;
            private readonly string branchName;

            public Internal(IGitCli cli, IRemoteRepositoryState repositoryState)
            {
                this.cli = cli;
                this.repositoryState = repositoryState;
                this.branchName = null;
            }

            public Internal(IGitCli cli, IRemoteRepositoryState repositoryState, string branchName)
            {
                this.cli = cli;
                this.repositoryState = repositoryState;
                this.branchName = branchName;
            }
            
            protected override async Task RunProcess()
            {
                if (branchName == null)
                {
                    await AppendProcess(cli.IsGitInitialized
                        ? cli.Fetch()
                        : cli.Clone());
                }
                else
                {

                    await AppendProcess(cli.Fetch(branchName));
                }
                repositoryState.RefreshAll();
            }
        }
    }
}
