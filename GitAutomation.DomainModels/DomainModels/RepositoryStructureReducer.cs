using System.Collections.Generic;
using System.Linq;

namespace GitAutomation.DomainModels
{
    public static class RepositoryStructureReducer
    {
        public static RepositoryStructure Reduce(this RepositoryStructure original, StandardAction action)
        {
            return action.Action switch
            {
                "StabilizeBranch" => StabilizeBranch(original, (string)action.Payload["Branch"]),
                "SetBranchState" => SetBranchState(original, (string)action.Payload["Branch"], (string)action.Payload["State"]),
                "SetOutputCommit" => SetOutputCommit(original, (string)action.Payload["Branch"], (string)action.Payload["OutputCommit"]),
                "SetMeta" => SetMeta(original, (string)action.Payload["Branch"], (IDictionary<string, object>)action.Payload["Meta"]),
                "RemoveReserve" => RemoveReserve(original, (string)action.Payload["Branch"]),
                _ => original
            };
        }

        private static RepositoryStructure StabilizeBranch(RepositoryStructure original, string branchReserveName) =>
            SetBranchState(original, branchReserveName, "Stable");

        private static RepositoryStructure SetBranchState(RepositoryStructure original, string branchReserveName, string status) =>
            original.SetBranchReserves(b => b.UpdateItem(branchReserveName, branch => branch.SetStatus(status)));

        private static RepositoryStructure SetOutputCommit(RepositoryStructure original, string branchReserveName, string lastCommit) =>
            original.SetBranchReserves(b => b.UpdateItem(branchReserveName, branch => branch.SetOutputCommit(lastCommit)));

        private static RepositoryStructure SetMeta(RepositoryStructure original, string branchReserveName, IDictionary<string, object> meta) =>
            original.SetBranchReserves(b => b.UpdateItem(branchReserveName, branch => branch.SetMeta(oldMeta => oldMeta.SetItems(meta))));

        private static RepositoryStructure RemoveReserve(RepositoryStructure original, string branchReserveName) =>
            original.SetBranchReserves(b =>
                b.Remove(branchReserveName)
                 .SetItems(b.Keys
                            .Where(k => b[k].Upstream.ContainsKey(branchReserveName))
                            .ToDictionary(
                                k => k, 
                                k => b[k].SetUpstream(upstream => upstream.Remove(branchReserveName))
                            )
                 )
            );
    }
}
