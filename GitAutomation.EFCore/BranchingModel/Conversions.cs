using GitAutomation.BranchSettings;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.EFCore.BranchingModel
{
    static class Conversions
    {

        public static BranchSettings.BranchGroup ToModel(this BranchGroup branch)
        {
            if (branch == null)
            {
                return null;
            }
            return new BranchSettings.BranchGroup
            {
                GroupName = branch.GroupName,
                UpstreamMergePolicy = Enum.TryParse<UpstreamMergePolicy>(branch.UpstreamMergePolicy, out var mergePolicy)
                    ? mergePolicy
                    : UpstreamMergePolicy.None,
                BranchType = Enum.TryParse<BranchGroupType>(branch.BranchType, out var branchType)
                    ? branchType
                    : BranchGroupType.Feature,
            };
        }

    }
}
