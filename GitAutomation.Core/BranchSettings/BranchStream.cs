using System;
using System.Collections.Generic;

namespace GitAutomation.BranchSettings
{
    public partial class BranchStream
    {
        public string DownstreamBranch { get; set; }
        public string UpstreamBranch { get; set; }

        public BranchGroup DownstreamBranchNavigation { get; set; }
        public BranchGroup UpstreamBranchNavigation { get; set; }
    }
}
