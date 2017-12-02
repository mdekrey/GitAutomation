using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.BranchSettings
{
    public class BranchGroup
    {
        public BranchGroup()
        {
        }

        public BranchGroup(BranchGroup original)
        {
            this.GroupName = original.GroupName;
            this.UpstreamMergePolicy = original.UpstreamMergePolicy;
            this.BranchType = original.BranchType;
        }

        public string GroupName { get; set; }
        public UpstreamMergePolicy UpstreamMergePolicy { get; set; } = UpstreamMergePolicy.None;
        public BranchGroupType BranchType { get; set; } = BranchGroupType.Feature;
    }

}
