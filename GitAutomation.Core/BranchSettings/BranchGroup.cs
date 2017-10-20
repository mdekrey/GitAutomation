using System;
using System.Collections.Generic;

namespace GitAutomation.BranchSettings
{
    public partial class BranchGroup
    {
        public BranchGroup()
        {
            UpstreamBranchConnections = new HashSet<BranchStream>();
            DownstreamBranchConnections = new HashSet<BranchStream>();
        }

        public string GroupName { get; set; }
        public bool RecreateFromUpstream { get; set; }
        public string BranchType { get; set; }

        public ICollection<BranchStream> UpstreamBranchConnections { get; set; }
        public ICollection<BranchStream> DownstreamBranchConnections { get; set; }
    }
}
