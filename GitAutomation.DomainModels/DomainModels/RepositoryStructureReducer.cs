using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
                "RepositoryStructure:CreateReserve" => CreateReserve(original, action.Payload),
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

        private static RepositoryStructure CreateReserve(RepositoryStructure original, Dictionary<string, object> payload)
        {
            var name = payload["Name"] as string;
            var type = payload["Type"] as string;
            var flowType = payload["FlowType"] as string;
            var upstream = (payload["Upstream"] as System.Collections.IEnumerable).Cast<string>();
            var originalBranch = payload["OriginalBranch"] as string;

            var result = original.SetBranchReserves(b => b.Add(name, new BranchReserve(
                reserveType: type,
                flowType: flowType,
                status: "OutOfDate",
                upstream: upstream.ToImmutableSortedDictionary(b => b, b => new UpstreamReserve(BranchReserve.EmptyCommit)),
                includedBranches: originalBranch == null ? ImmutableSortedDictionary<string, BranchReserveBranch>.Empty
                    : new Dictionary<string, BranchReserveBranch>
                    {
                        { originalBranch, new BranchReserveBranch(BranchReserve.EmptyCommit) }
                    }.ToImmutableSortedDictionary(),
                outputCommit: BranchReserve.EmptyCommit,
                meta: ImmutableSortedDictionary<string, object>.Empty
            )));
            if (result.GetValidationErrors().Any())
            {
                return original;
            }
            return result;
        }

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
