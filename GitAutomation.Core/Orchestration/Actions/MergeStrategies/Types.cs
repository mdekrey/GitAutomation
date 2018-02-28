using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Orchestration.Actions.MergeStrategies
{
    public enum MergeConflictResolution
    {
        AddIntegrationBranch,
        PendingIntegrationBranch,
        PullRequest,
        PendingPullRequest,
    }

    public struct MergeStatus
    {
        public bool HadConflicts;
        public MergeConflictResolution Resolution;
        public string BadReason;
    }

    public struct NeededMerge
    {
        public string GroupName;
        public string BranchName;
    }

}
