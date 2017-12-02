using GitAutomation.BranchSettings;
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
        Task AfterCreate(string latestBranchName, string branchName);
    }
}
