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

            public Internal(IGitCli cli)
            {
                this.cli = cli;
            }

            protected override async Task RunProcess()
            {
                await cli.EnsureInitialized;
            }
        }
    }
}
