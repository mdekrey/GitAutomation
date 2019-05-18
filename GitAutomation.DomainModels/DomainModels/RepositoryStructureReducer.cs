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
                "RepositoryStructure:StabilizeReserve" => StabilizeReserve(original, (string)action.Payload["Reserve"]),
                "RepositoryStructure:SetReserveState" => SetBranchState(original, (string)action.Payload["Reserve"], (string)action.Payload["State"]),
                "RepositoryStructure:SetOutputCommit" => SetOutputCommit(original, (string)action.Payload["Reserve"], (string)action.Payload["OutputCommit"]),
                "RepositoryStructure:SetMeta" => SetMeta(original, (string)action.Payload["Reserve"], (IDictionary<string, object>)action.Payload["Meta"]),
                "RepositoryStructure:RemoveReserve" => RemoveReserve(original, (string)action.Payload["Reserve"]),
                _ => original
            };
        }

        private static RepositoryStructure StabilizeReserve(RepositoryStructure original, string branchReserveName) =>
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
