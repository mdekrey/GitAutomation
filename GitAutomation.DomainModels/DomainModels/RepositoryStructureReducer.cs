using GitAutomation.DomainModels.Actions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace GitAutomation.DomainModels
{
    public static class RepositoryStructureReducer
    {
        public static RepositoryStructure Reduce(this RepositoryStructure original, IStandardAction a)
        {
            return a switch
            {
                // Basic reducers
                StabilizeReserveAction action => StabilizeReserve(original, (string)action.Reserve),
                SetReserveStateAction action => SetReserveState(original, (string)action.Reserve, (string)action.State),
                SetOutputCommitAction action => SetOutputCommit(original, (string)action.Reserve, (string)action.OutputCommit),
                SetMetaAction action => SetMeta(original, (string)action.Reserve, action.Meta),
                CreateReserveAction action => CreateReserve(original, action),
                RemoveReserveAction action => RemoveReserve(original, (string)action.Reserve),
                // Complex reducers
                SetReserveOutOfDateAction action => SetReserveOutOfDate(original, action),
                StabilizeNoUpstreamAction action => StabilizeNoUpstream(original, action),
                StabilizePushedReserveAction action => PushedReserve(original, action),
                StabilizeRemoteUpdatedReserveAction action => ReceivedReserve(original, action),
                CouldNotPushAction action => CouldNotPush(original, action),
                ManualInterventionNeededAction action => ManualInterventionNeeded(original, action),
                RefsAction _ => ClearPushOnReserves(original),
                _ => original
            };
        }

        private static RepositoryStructure StabilizeReserve(RepositoryStructure original, string branchReserveName) =>
            SetReserveState(original, branchReserveName, "Stable");

        private static RepositoryStructure SetReserveState(RepositoryStructure original, string branchReserveName, string status) =>
            original.SetBranchReserves(b => b.UpdateItem(branchReserveName, branch => branch.SetStatus(status)));

        private static RepositoryStructure SetOutputCommit(RepositoryStructure original, string branchReserveName, string lastCommit) =>
            original.SetBranchReserves(b => b.UpdateItem(branchReserveName, branch => branch.SetOutputCommit(lastCommit)));

        private static RepositoryStructure SetMeta(RepositoryStructure original, string branchReserveName, IDictionary<string, string> meta) =>
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
                    var outputBranch = targetReserve.IncludedBranches.Where(branch => branch.Value.Meta.TryGetValue("Role", out var role) && (string)role == "Output").ToArray();
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
                    meta: ImmutableSortedDictionary<string, string>.Empty))
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

        private static RepositoryStructure CreateReserve(RepositoryStructure original, CreateReserveAction action)
        {
            var result = Chain(original, n => AddReserve(n, action.Name, action.Type, action.FlowType));
            result = action.Upstream.Aggregate(result, (n, upstream) => AddUpstreamToReserve(n, action.Name, upstream));
            if (action.OriginalBranch != null)
            {
                result = AddOutputBranch(result, action.Name, action.OriginalBranch, BranchReserve.EmptyCommit);
            }
            return result;
        }

        private static RepositoryStructure AddOutputBranch(RepositoryStructure result, string name, string originalBranch, string emptyCommit) =>
            result.SetBranchReserves(reserves => reserves.UpdateItem(name, r => r.SetIncludedBranches(b => b.Add(originalBranch, CreateOutputBranchReserveBranch(emptyCommit)))));

        private static RepositoryStructure SetReserveOutOfDate(RepositoryStructure original, SetReserveOutOfDateAction action) =>
            Chain(original,
                n => SetReserveState(n, action.Reserve, "OutOfDate")
                );

        private static RepositoryStructure StabilizeNoUpstream(RepositoryStructure original, StabilizeNoUpstreamAction action) =>
            Chain(original,
                n => SetReserveState(n, action.Reserve, "Stable"),
                n => SetBranchCommits(n, action.Reserve, action.BranchCommits),
                n => SetOutputCommitToBranch(n, action.Reserve)
                );

        private static RepositoryStructure PushedReserve(RepositoryStructure original, StabilizePushedReserveAction action) =>
            Chain(original,
                n => SetReserveState(n, action.Reserve, "Pushed"),
                n => action.NewOutput == null ? n : AddOutputBranch(n, action.Reserve, action.NewOutput, BranchReserve.EmptyCommit),
                n => SetBranchCommits(n, action.Reserve, action.BranchCommits),
                n => SetUpstreamCommits(n, action.Reserve, action.ReserveOutputCommits),
                n => SetOutputCommitToBranch(n, action.Reserve)
            );

        private static RepositoryStructure ReceivedReserve(RepositoryStructure original, StabilizeRemoteUpdatedReserveAction action) =>
            Chain(original,
                n => SetReserveState(n, action.Reserve, "Stable"),
                n => SetBranchCommits(n, action.Reserve, action.BranchCommits),
                n => SetUpstreamCommits(n, action.Reserve, action.ReserveOutputCommits),
                n => SetOutputCommitToBranch(n, action.Reserve)
            );

        private static RepositoryStructure CouldNotPush(RepositoryStructure original, CouldNotPushAction action) =>
            Chain(original,
                n => SetBranchCommits(n, action.Reserve, action.BranchCommits),
                n => SetUpstreamCommits(n, action.Reserve, action.ReserveOutputCommits),
                n => SetReserveState(n, action.Reserve, "CouldNotPush")
            );

        private static RepositoryStructure ManualInterventionNeeded(RepositoryStructure original, ManualInterventionNeededAction action) =>
            Chain(original,
                n => SetReserveState(n, action.Reserve, action.State),
                n => action.NewBranches.Aggregate(n, (prev, branch) =>
                    AddIncludedBranch(prev, new AddIncludedBranchAction
                    {
                        Reserve = action.Reserve,
                        Name = branch.Name,
                        Commit = branch.Commit,
                        Meta = new Dictionary<string, string> { { "Role", branch.Role }, { "Source", branch.Source } }
                    })
                ),
                n => SetBranchCommits(n, action.Reserve, action.BranchCommits),
                n => SetUpstreamCommits(n, action.Reserve, action.ReserveOutputCommits),
                n => SetOutputCommitToBranch(n, action.Reserve)
            );

        private static RepositoryStructure AddIncludedBranch(RepositoryStructure original, AddIncludedBranchAction payload) =>
            original.SetBranchReserves(n => n.UpdateItem(payload.Reserve, reserve =>
            {
                return reserve.SetIncludedBranches(branches => branches.SetItem(payload.Name, CreateBranchReserveBranch(payload.Commit, payload.Meta)));
            }));

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
