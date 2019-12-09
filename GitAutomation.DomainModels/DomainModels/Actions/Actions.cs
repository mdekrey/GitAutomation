using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.DomainModels.Actions
{
    public struct StabilizeReserveAction : IStandardAction
    {
        public string Reserve { get; set; }
    }

    public struct SetReserveStateAction : IStandardAction
    {
        public string Reserve { get; set; }
        public string State { get; set; }
    }

    public struct SetOutputCommitAction : IStandardAction
    {
        public string Reserve { get; set; }
        public string OutputCommit { get; set; }
    }

    public struct SetMetaAction : IStandardAction
    {
        public string Reserve { get; set; }
        public IDictionary<string, string> Meta { get; set; }
    }

    public struct CreateReserveAction : IStandardAction
    {
        public string Name { get; set; } // = "";
        public string Type { get; set; } // = "";
        public string FlowType { get; set; } // = "";
        public string[] Upstream { get; set; } // = Array.Empty<string>();
        public string? OriginalBranch { get; set; }
    }

    public struct RemoveReserveAction : IStandardAction
    {
        public string Reserve { get; set; }
    }

    public struct SetReserveOutOfDateAction : IStandardAction
    {
        public string Reserve { get; set; }
    }

    public struct StabilizeNoUpstreamAction : IStandardAction
    {
        public string Reserve { get; set; }
        public Dictionary<string, string> BranchCommits { get; set; }
    }

    public struct StabilizePushedReserveAction : IStandardAction
    {
        public string Reserve { get; set; }
        public Dictionary<string, string> BranchCommits { get; set; }
        public Dictionary<string, string> ReserveOutputCommits { get; set; }
        public string? NewOutput { get; set; }
    }

    public struct CouldNotPushAction : IStandardAction
    {
        public string Reserve { get; set; }
        public Dictionary<string, string> BranchCommits { get; set; }
        public Dictionary<string, string> ReserveOutputCommits { get; set; }
    }

    public struct ManualInterventionNeededAction : IStandardAction
    {
        public string Reserve { get; set; }
        public string State { get; set; }
        public ManualInterventionBranch[] NewBranches { get; set; }
        public Dictionary<string, string> BranchCommits { get; set; }
        public Dictionary<string, string> ReserveOutputCommits { get; set; }

        public struct ManualInterventionBranch
        {
            public string Name { get; set; }
            public string Commit { get; set; }
            public string Role { get; set; }
            public string Source { get; set; }
        }
    }

    public struct AddIncludedBranchAction : IStandardAction
    {
        public string Reserve { get; set; }
        public string Name { get; set; }
        public string Commit { get; set; }
        public Dictionary<string, string> Meta { get; set; }
    }
}
