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
                // Basic reducers
                "RepositoryStructure:StabilizeReserve" => StabilizeReserve(original, (string)action.Payload["Reserve"]),
                "RepositoryStructure:SetReserveState" => SetReserveState(original, (string)action.Payload["Reserve"], (string)action.Payload["State"]),
                "RepositoryStructure:SetOutputCommit" => SetOutputCommit(original, (string)action.Payload["Reserve"], (string)action.Payload["OutputCommit"]),
                "RepositoryStructure:SetMeta" => SetMeta(original, (string)action.Payload["Reserve"], action.Payload["Meta"].ToObject<Dictionary<string, object>>()),
                "RepositoryStructure:CreateReserve" => CreateReserve(original, action.Payload.ToObject<CreateReservePayload>()),
                "RepositoryStructure:RemoveReserve" => RemoveReserve(original, (string)action.Payload["Reserve"]),
                // Complex reducers
                "RepositoryStructure:SetOutOfDate" => SetReserveOutOfDate(original, action.Payload.ToObject<SetReserveOutOfDatePayload>()),
                "RepositoryStructure:StabilizeNoUpstream" => StabilizeNoUpstream(original, action.Payload.ToObject<StabilizeNoUpstreamPayload>()),
                "RepositoryStructure:PushedReserve" => PushedReserve(original, action.Payload.ToObject<StabilizePushedReservePayload>()),
                "RepositoryStructure:CouldNotPush" => CouldNotPush(original, action.Payload.ToObject<CouldNotPushPayload>()),
                "RepositoryStructure:ManualInterventionNeeded" => ManualInterventionNeeded(original, action.Payload.ToObject<ManualInterventionNeededPayload>()),
                "TargetRepository:Refs" => ClearPushOnReserves(original),
                _ => original
            };
        }

        private static RepositoryStructure StabilizeReserve(RepositoryStructure original, string branchReserveName) =>
            SetReserveState(original, branchReserveName, "Stable");

        private static RepositoryStructure SetReserveState(RepositoryStructure original, string branchReserveName, string status) =>
            original.SetBranchReserves(b => b.UpdateItem(branchReserveName, branch => branch.SetStatus(status)));

        private static RepositoryStructure SetOutputCommit(RepositoryStructure original, string branchReserveName, string lastCommit) =>
            original.SetBranchReserves(b => b.UpdateItem(branchReserveName, branch => branch.SetOutputCommit(lastCommit)));

        private static RepositoryStructure SetMeta(RepositoryStructure original, string branchReserveName, IDictionary<string, object> meta) =>
            original.SetBranchReserves(b => b.UpdateItem(branchReserveName, branch => branch.SetMeta(oldMeta => oldMeta.SetItems(meta))));

        // TODO - make a chainable reducer
        // TODO - make these all callable via the reducer
        private static RepositoryStructure SetUpstreamCommits(RepositoryStructure original, string reserve, Dictionary<string, string> reserveOutputCommits) =>
            original.SetBranchReserves(br =>
                br.UpdateItem(reserve, reserve =>
                    reserve
                        .SetUpstream(reserves => reserveOutputCommits.Aggregate(reserves, (r, kvp) => r.SetItem(kvp.Key, r[kvp.Key].SetLastOutput(kvp.Value))))
                )
            );

        private static RepositoryStructure SetBranchCommits(RepositoryStructure original, string reserve, Dictionary<string, string> branchCommits) =>
            original.SetBranchReserves(br =>
                br.UpdateItem(reserve, reserve =>
                    reserve.SetIncludedBranches(branches =>
                        branchCommits.Aggregate(branches, (b, kvp) => b.SetItem(kvp.Key, b[kvp.Key].SetLastCommit(kvp.Value)))
                    )
                )
            );

        private static RepositoryStructure SetOutputCommitToBranch(RepositoryStructure original, string reserve) =>
            original.SetBranchReserves(br =>
                br.UpdateItem(reserve, targetReserve =>
                {
                    var outputBranch = targetReserve.IncludedBranches.Where(branch => branch.Value.Meta.TryGetValue("Role", out var role) && role == "Output").ToArray();
                    if (outputBranch.Length == 1)
                    {
                        targetReserve = targetReserve.SetOutputCommit(outputBranch[0].Value.LastCommit);
                    }
                    return targetReserve;
                })
            );

        private static RepositoryStructure AddUpstreamToReserve(RepositoryStructure original, string branchReserveName, string upstream) =>
            original.SetBranchReserves(b =>
                b.UpdateItem(branchReserveName, r => r.SetUpstream(d => d.Add(upstream, new UpstreamReserve(BranchReserve.EmptyCommit))))
            );

        private static RepositoryStructure AddReserve(RepositoryStructure original, string branchReserveName, string reserveType, string flowType) =>
            original.SetBranchReserves(b =>
                b.Add(branchReserveName, new BranchReserve(reserveType,
                    flowType: flowType,
                    status: "Stable",
                    upstream: ImmutableSortedDictionary<string, UpstreamReserve>.Empty,
                    includedBranches: ImmutableSortedDictionary<string, BranchReserveBranch>.Empty,
                    outputCommit: BranchReserve.EmptyCommit,
                    meta: ImmutableSortedDictionary<string, object>.Empty))
            );

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

        private static RepositoryStructure ClearPushOnReserves(RepositoryStructure original) =>
            original.SetBranchReserves(b =>
                b.SetItems(b.Keys
                    .Where(k => b[k].Status == "Pushed")
                    .ToDictionary(
                        k => k,
                        k => b[k].SetStatus("Stable")
                    )));

        class CreateReservePayload
        {
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
            public string FlowType { get; set; } = "";
            public string[] Upstream { get; set; } = Array.Empty<string>();
            public string? OriginalBranch { get; set; }
        }

        private static RepositoryStructure CreateReserve(RepositoryStructure original, CreateReservePayload data)
        {
            var result = Chain(original, n => AddReserve(n, data.Name, data.Type, data.FlowType));
            result = data.Upstream.Aggregate(result, (n, upstream) => AddUpstreamToReserve(n, data.Name, upstream));
            if (data.OriginalBranch != null)
            {
                result = AddOutputBranch(result, data.Name, data.OriginalBranch, BranchReserve.EmptyCommit);
            }
            return result;
        }

        private static RepositoryStructure AddOutputBranch(RepositoryStructure result, string name, string originalBranch, string emptyCommit) =>
            result.SetBranchReserves(reserves => reserves.UpdateItem(name, r => r.SetIncludedBranches(b => b.Add(originalBranch, CreateOutputBranchReserveBranch(emptyCommit)))));

        class SetReserveOutOfDatePayload
        {
#nullable disable
            public string Reserve { get; set; }
#nullable restore
        }

        private static RepositoryStructure SetReserveOutOfDate(RepositoryStructure original, SetReserveOutOfDatePayload payload) =>
            Chain(original,
                n => SetReserveState(n, payload.Reserve, "OutOfDate")
                );

        class StabilizeNoUpstreamPayload
        {
#nullable disable
            public string Reserve { get; set; }
            public Dictionary<string, string> BranchCommits { get; set; }
        }

        private static RepositoryStructure StabilizeNoUpstream(RepositoryStructure original, StabilizeNoUpstreamPayload payload) =>
            Chain(original,
                n => SetReserveState(n, payload.Reserve, "Stable"),
                n => SetBranchCommits(n, payload.Reserve, payload.BranchCommits),
                n => SetOutputCommitToBranch(n, payload.Reserve)
                );

#nullable restore

        class StabilizePushedReservePayload
        {
#nullable disable
            public string Reserve { get; set; }
            public Dictionary<string, string> BranchCommits { get; set; }
            public Dictionary<string, string> ReserveOutputCommits { get; set; }
            public string NewOutput { get; set; }
        }

        private static RepositoryStructure PushedReserve(RepositoryStructure original, StabilizePushedReservePayload payload) =>
            Chain(original,
                n => SetReserveState(n, payload.Reserve, "Pushed"),
                n => payload.NewOutput == null ? n : AddOutputBranch(n, payload.Reserve, payload.NewOutput, BranchReserve.EmptyCommit),
                n => SetBranchCommits(n, payload.Reserve, payload.BranchCommits),
                n => SetUpstreamCommits(n, payload.Reserve, payload.ReserveOutputCommits),
                n => SetOutputCommitToBranch(n, payload.Reserve)
            );
#nullable restore

        class CouldNotPushPayload
        {
#nullable disable
            public string Reserve { get; set; }
            public Dictionary<string, string> BranchCommits { get; set; }
            public Dictionary<string, string> ReserveOutputCommits { get; set; }
        }

        private static RepositoryStructure CouldNotPush(RepositoryStructure original, CouldNotPushPayload payload) =>
            Chain(original,
                n => SetBranchCommits(n, payload.Reserve, payload.BranchCommits),
                n => SetUpstreamCommits(n, payload.Reserve, payload.ReserveOutputCommits),
                n => SetReserveState(n, payload.Reserve, "CouldNotPush")
            );
#nullable restore

        class ManualInterventionNeededPayload
        {
#nullable disable
            public string Reserve { get; set; }
            public string State { get; set; }
            public ManualInterventionBranch[] NewBranches { get; set; }
            public Dictionary<string, string> BranchCommits { get; set; }
            public Dictionary<string, string> ReserveOutputCommits { get; set; }

            internal class ManualInterventionBranch
            {
                public string Name { get; set; }
                public string Commit { get; set; }
                public string Role { get; set; }
                public string Source { get; set; }
            }
        }

        private static RepositoryStructure ManualInterventionNeeded(RepositoryStructure original, ManualInterventionNeededPayload payload) =>
            Chain(original,
                n => SetReserveState(n, payload.Reserve, payload.State),
                n => payload.NewBranches.Aggregate(n, (prev, branch) =>
                    AddIncludedBranch(prev, new AddIncludedBranchPayload
                    {
                        Reserve = payload.Reserve,
                        Name = branch.Name,
                        Commit = branch.Commit,
                        Meta = new Dictionary<string, string> { { "Role", branch.Role }, { "Source", branch.Source } }
                    })
                ),
                n => SetBranchCommits(n, payload.Reserve, payload.BranchCommits),
                n => SetUpstreamCommits(n, payload.Reserve, payload.ReserveOutputCommits)
            );
#nullable restore

        class AddIncludedBranchPayload
        {
#nullable disable
            public string Reserve { get; set; }
            public string Name { get; set; }
            public string Commit { get; set; }
            public Dictionary<string, string> Meta { get; set; }
        }

        private static RepositoryStructure AddIncludedBranch(RepositoryStructure original, AddIncludedBranchPayload payload) =>
            original.SetBranchReserves(n => n.UpdateItem(payload.Reserve, reserve =>
            {
                return reserve.SetIncludedBranches(branches => branches.SetItem(payload.Name, CreateBranchReserveBranch(payload.Commit, payload.Meta)));
            }));
#nullable restore

        private static BranchReserveBranch CreateOutputBranchReserveBranch(string commit)
        {
            return CreateBranchReserveBranch(
                commit,
                new Dictionary<string, string>()
                {
                    { "Role", "Output" }
                }
            );
        }

        private static BranchReserveBranch CreateBranchReserveBranch(string commit, Dictionary<string, string> meta)
        {
            return new BranchReserveBranch(
                commit,
                meta.ToImmutableSortedDictionary()
            );
        }

        private static RepositoryStructure Chain(RepositoryStructure original, params Func<RepositoryStructure, RepositoryStructure>[] operations)
        {
            return operations.Aggregate(original, (current, operation) => operation(current));
        }
    }
}
