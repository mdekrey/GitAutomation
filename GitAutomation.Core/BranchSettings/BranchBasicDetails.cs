using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.BranchSettings
{
    public class BranchBasicDetails
    {
        public bool RecreateFromUpstream { get; set; }
        public bool IsServiceLine { get; set; }
        public string ConflictResolutionMode { get; set; }
    }

}
