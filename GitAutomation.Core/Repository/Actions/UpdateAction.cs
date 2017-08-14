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
    class UpdateAction : CliAction
    {
        public override string ActionType => "Update";

        protected override IReactiveProcess GetCliAction(GitCli gitCli) =>
            gitCli.IsGitInitialized
                ? gitCli.Fetch()
                : gitCli.Clone();
    }
}
