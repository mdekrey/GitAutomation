using GitAutomation.Processes;
using GitAutomation.Repository;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace GitAutomation.Orchestration.Actions
{
    class UpdateAction : CliAction, IUniqueAction
    {
        public override string ActionType => "Update";

        public void AbortAs(IObservable<OutputMessage> otherStream)
        {
            this.Abort(otherStream);
        }

        protected override IReactiveProcess GetCliAction(GitCli gitCli) =>
            gitCli.IsGitInitialized
                ? gitCli.Fetch()
                : gitCli.Clone();
    }
}
