using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.BranchSettings
{
    public class BranchBasicDetails
    {
        public bool RecreateFromUpstream { get; set; }
        public BranchType BranchType { get; set; }
    }

}
