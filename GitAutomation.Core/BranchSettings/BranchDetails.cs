using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.BranchSettings
{
    public class BranchDetails : BranchBasicDetails
    {
        public BranchDetails()
        {
        }

        public BranchDetails(BranchDetails original)
            : base(original)
        {
            this.DirectDownstreamBranches = original.DirectDownstreamBranches;
            this.DownstreamBranches = original.DownstreamBranches;
            this.DirectUpstreamBranches = original.DirectUpstreamBranches;
            this.UpstreamBranches = original.UpstreamBranches;
        }

        public ImmutableList<BranchBasicDetails> DirectDownstreamBranches { get; set; }
        public ImmutableList<BranchBasicDetails> DownstreamBranches { get; set; }
        public ImmutableList<BranchBasicDetails> DirectUpstreamBranches { get; set; }
        public ImmutableList<BranchBasicDetails> UpstreamBranches { get; set; }
    }
}
