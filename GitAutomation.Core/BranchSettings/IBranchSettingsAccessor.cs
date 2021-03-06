﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.BranchSettings
{
    public interface IBranchSettingsAccessor
    {
        Task<ImmutableList<BranchGroup>> GetAllBranchGroups();
        Task<ImmutableDictionary<string, BranchGroup>> GetBranchGroups(params string[] groupNames);
        Task<ImmutableDictionary<string, ImmutableList<string>>> GetDownstreamBranchGroups(params string[] groupNames);
        Task<ImmutableDictionary<string, ImmutableList<string>>> GetUpstreamBranchGroups(params string[] groupNames);

        Task<Consolidation> CalculateConsolidation(IEnumerable<string> branchesToRemove, string targetBranch);
    }
}
