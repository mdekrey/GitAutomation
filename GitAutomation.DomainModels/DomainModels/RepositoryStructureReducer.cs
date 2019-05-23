using Newtonsoft.Json.Linq;
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
                "RepositoryStructure:SetMeta" => SetMeta(original, (string)action.Payload["Reserve"], action.Payload["Meta"].ToObject<Dictionary<string, object>>()),
                "RepositoryStructure:CreateReserve" => CreateReserve(original, action.Payload.ToObject<CreateReservePayload>()),
                "RepositoryStructure:RemoveReserve" => RemoveReserve(original, (string)action.Payload["Reserve"]),
                "RepositoryStructure:SetOutOfDate" => SetReserveOutOfDate(original, action.Payload.ToObject<SetReserveOutOfDatePayload>()),
                "RepositoryStructure:StabilizeNoUpstream" => StabilizeNoUpstream(original, action.Payload.ToObject<StabilizeNoUpstreamPayload>()),
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


        class CreateReservePayload
        {
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
            public string FlowType { get; set; } = "";
            public string[] Upstream { get; set; } = Array.Empty<string>();
            public string OriginalBranch { get; set; } = "";
        }

        private static RepositoryStructure CreateReserve(RepositoryStructure original, CreateReservePayload data)
        {
            // TODO - add owner meta
            var result = original.SetBranchReserves(b => b.Add(data.Name, new BranchReserve(
                reserveType: data.Type,
                flowType: data.FlowType,
                status: "Stable",
                upstream: data.Upstream.ToImmutableSortedDictionary(b => b, b => new UpstreamReserve(BranchReserve.EmptyCommit)),
                includedBranches: data.OriginalBranch == null ? ImmutableSortedDictionary<string, BranchReserveBranch>.Empty
                    : new Dictionary<string, BranchReserveBranch>
                    {
                        { data.OriginalBranch, new BranchReserveBranch(
                            BranchReserve.EmptyCommit, 
                            new Dictionary<string, string>()
                            {
                                { "Role", "Output" }
                            }.ToImmutableSortedDictionary()
                        ) }
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

        class SetReserveOutOfDatePayload
        {
#nullable disable
            public string Reserve { get; set; }
            public Dictionary<string, string> BranchCommits { get; set; }
            public Dictionary<string, string> ReserveOutputCommits { get; set; }
#nullable restore
        }

        private static RepositoryStructure SetReserveOutOfDate(RepositoryStructure original, SetReserveOutOfDatePayload payload) =>
            original.SetBranchReserves(br =>
                br.SetItem(payload.Reserve, br[payload.Reserve]
                    .SetStatus("OutOfDate")
                    .SetIncludedBranches(branches => payload.BranchCommits.Aggregate(branches, (b, kvp) => b.SetItem(kvp.Key, b[kvp.Key].SetLastCommit(kvp.Value))))
                    .SetUpstream(reserves => payload.ReserveOutputCommits.Aggregate(reserves, (r, kvp) => r.SetItem(kvp.Key, r[kvp.Key].SetLastOutput(kvp.Value))))
                    )
            );

        class StabilizeNoUpstreamPayload
        {
#nullable disable
            public string Reserve { get; set; }
            public Dictionary<string, string> BranchCommits { get; set; }
        }

        private static RepositoryStructure StabilizeNoUpstream(RepositoryStructure original, StabilizeNoUpstreamPayload payload) =>
            original.SetBranchReserves(br => {
                var targetReserve = br[payload.Reserve]
                    .SetStatus("Stable")
                    .SetIncludedBranches(branches => payload.BranchCommits.Aggregate(branches, (b, kvp) => b.SetItem(kvp.Key, b[kvp.Key].SetLastCommit(kvp.Value))));
                var outputBranch = targetReserve.IncludedBranches.Where(branch => branch.Value.Meta.TryGetValue("Role", out var role) && role == "Output").ToArray();
                if (outputBranch.Length == 1)
                {
                    targetReserve = targetReserve.SetOutputCommit(outputBranch[0].Value.LastCommit);
                }
                return br.SetItem(payload.Reserve, targetReserve);
#nullable restore
            });
    }
}
