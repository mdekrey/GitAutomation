using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using GitAutomation.GitService;

namespace GitAutomation.BranchSettings
{
    public class BranchBasicDetails
    {
        public BranchBasicDetails()
        {
        }

        public BranchBasicDetails(BranchBasicDetails original)
        {
            this.BranchName = original.BranchName;
            this.RecreateFromUpstream = original.RecreateFromUpstream;
            this.BranchType = original.BranchType;
            this.BranchNames = original.BranchNames;
        }

        public string BranchName { get; set; }
        public bool RecreateFromUpstream { get; set; } = false;
        public BranchType BranchType { get; set; } = BranchType.Feature;

        public ImmutableList<string> BranchNames { get; set; }
        public ImmutableList<CommitStatus> Statuses { get; internal set; }
    }

}
