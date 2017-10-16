using System;
using System.Collections.Generic;

namespace GitAutomation.EFCore.BranchingModel
{
    public partial class BranchGroup
    {
        public BranchGroup()
        {
            BranchStreamDownstreamBranchNavigation = new HashSet<BranchStream>();
            BranchStreamUpstreamBranchNavigation = new HashSet<BranchStream>();
        }

        public string GroupName { get; set; }
        public bool RecreateFromUpstream { get; set; }
        public string BranchType { get; set; }

        public ICollection<BranchStream> BranchStreamDownstreamBranchNavigation { get; set; }
        public ICollection<BranchStream> BranchStreamUpstreamBranchNavigation { get; set; }
    }
}
