using GitAutomation.BranchSettings;
using GitAutomation.Processes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Orchestration.Actions.MergeStrategies
{
    public interface IMergeStrategy
    {
        Task<bool> NeedsCreate(string latestBranchName, ImmutableList<NeededMerge> upstreamBranches);

        Task<ImmutableList<NeededMerge>> FindNeededMerges(string latestBranchName, ImmutableList<NeededMerge> upstreamBranches);
        Task AfterCreate(BranchGroup group, string latestBranchName, string createdBranchName, Func<IReactiveProcess, RepositoryActionReactiveProcessEntry> appendProcess);
    }
}
