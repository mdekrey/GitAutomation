using GitAutomation.Processes;
using GitAutomation.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Orchestration.Actions
{
    class GetRemoteBranchesAction : CliAction
    {
        public override string ActionType => "GetRemoteBranches";

        protected override IReactiveProcess GetCliAction(GitCli gitCli) =>
            gitCli.IsGitInitialized
                ? gitCli.GetRemoteBranches()
                : gitCli.GetRemoteBranches();
    }
}
