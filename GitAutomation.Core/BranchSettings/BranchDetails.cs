using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.BranchSettings
{
    public class BranchDetails
    {
        public string BranchName { get; set; }

        public bool RecreateFromUpstream { get; set; }
        public bool IsServiceLine { get; set; }

        public ImmutableList<string> DirectDownstreamBranches { get; set; }
        public ImmutableList<string> DownstreamBranches { get; set; }
        public ImmutableList<string> DirectUpstreamBranches { get; set; }
        public ImmutableList<string> UpstreamBranches { get; set; }
    }
}
