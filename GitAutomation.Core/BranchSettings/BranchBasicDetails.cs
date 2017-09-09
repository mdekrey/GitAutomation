using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.BranchSettings
{
    public class BranchBasicDetails
    {
        public string BranchName { get; set; }
        public bool RecreateFromUpstream { get; set; } = false;
        public BranchType BranchType { get; set; } = BranchType.Feature;

        public ImmutableList<string> BranchNames { get; set; }
    }

}
