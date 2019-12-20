using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Actions;
using GitAutomation.Web;
using LibGit2Sharp;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Branches.Common.Steps
{
    public class TemporaryIntegrationBranchLifecycle
    {
        private readonly AutomationOptions automationOptions;
        private readonly TargetRepositoryOptions options;
        private readonly IBranchNaming branchNaming;

        public TemporaryIntegrationBranchLifecycle(IOptions<TargetRepositoryOptions> options, IOptions<AutomationOptions> automationOptions, IBranchNaming branchNaming)
        {
            this.options = options.Value;
            this.automationOptions = automationOptions.Value;
            this.branchNaming = branchNaming;
        }

        public ManualInterventionNeededAction.ManualInterventionBranch[] Create(ReserveScriptParameters parameters, Repository repo, IEnumerable<string> upstreamNeeded)
        {
            var defaultRemote = automationOptions.DefaultRemote;

            // TODO - temporary integration branches should be removed somewhere - maybe when it becomes stable?
            var newBranches = upstreamNeeded.Select(entry =>
            {
                var commit = parameters.UpstreamReserves[entry].OutputCommit;
                var newBranchName = branchNaming.GenerateIntegrationBranchName(parameters.Name, entry);
                var remoteBranchName = branchNaming.GetCheckoutRepositoryBranchName(defaultRemote, newBranchName);
                if (repo.Branches[remoteBranchName] != null && (!parameters.Reserve.IncludedBranches.ContainsKey(remoteBranchName) || parameters.Reserve.IncludedBranches[remoteBranchName].Meta["Role"] != "Integration"))
                {
                    // TODO - should warn or something here
                    return default;
                }
                repo.CreateBranch(remoteBranchName, commit);
                return new ManualInterventionNeededAction.ManualInterventionBranch
                {
                    Commit = commit,
                    Name = remoteBranchName,
                    Role = "Integration",
                    Source = entry
                };
            }).Where(b => b.Name != null).ToArray();
            var refspecs = newBranches.Select(b => $"refs/heads/{b.Name}:refs/heads/{b.Name.Substring(defaultRemote.Length + 1)}").ToArray();
            repo.EnsureRemoteAndPush(defaultRemote, options, refspecs);
            return newBranches;
        }

    }
}
