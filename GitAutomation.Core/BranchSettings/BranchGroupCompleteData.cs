﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.BranchSettings
{
    public class BranchGroupCompleteData : BranchGroupDetails
    {
        public BranchGroupCompleteData()
        {
        }

        public BranchGroupCompleteData(BranchGroupDetails original)
            : base(original)
        {
        }

        public BranchGroupCompleteData(BranchGroupCompleteData original)
            : base(original)
        {
            this.BranchNames = original.BranchNames;
            this.DirectDownstreamBranchGroups = original.DirectDownstreamBranchGroups;
            this.DownstreamBranchGroups = original.DownstreamBranchGroups;
            this.DirectUpstreamBranchGroups = original.DirectUpstreamBranchGroups;
            this.UpstreamBranchGroups = original.UpstreamBranchGroups;
            this.HierarchyDepth = original.HierarchyDepth;
        }

        public ImmutableList<string> BranchNames { get; set; }

        public ImmutableList<string> DirectDownstreamBranchGroups { get; set; }
        public ImmutableList<string> DownstreamBranchGroups { get; set; }
        public ImmutableList<string> DirectUpstreamBranchGroups { get; set; }
        public ImmutableList<string> UpstreamBranchGroups { get; set; }

        public int HierarchyDepth { get; set; }

    }
}
