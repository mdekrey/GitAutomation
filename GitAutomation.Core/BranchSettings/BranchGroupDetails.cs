using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.BranchSettings
{
    public class BranchGroupDetails
    {
        public BranchGroupDetails()
        {
        }

        public BranchGroupDetails(BranchGroupDetails original)
        {
            this.GroupName = original.GroupName;
            this.RecreateFromUpstream = original.RecreateFromUpstream;
            this.BranchType = original.BranchType;
        }

        public string GroupName { get; set; }
        public bool RecreateFromUpstream { get; set; } = false;
        public BranchGroupType BranchType { get; set; } = BranchGroupType.Feature;
    }

}
