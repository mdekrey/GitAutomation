using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.BranchSettings
{

    public class BranchHierarchyDetails : BranchBasicDetails
    {
        public BranchHierarchyDetails()
        {
        }
        public BranchHierarchyDetails(BranchDepthDetails original)
        {
            this.BranchName = original.BranchName;
            this.BranchType = original.BranchType;
            this.RecreateFromUpstream = original.RecreateFromUpstream;
            this.HierarchyDepth = original.Ordinal;
        }

        public ImmutableList<string> DownstreamBranches { get; set; } = ImmutableList<string>.Empty;
        public int HierarchyDepth { get; set; }
    }
}
