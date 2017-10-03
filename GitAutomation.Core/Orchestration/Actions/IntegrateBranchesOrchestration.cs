using GitAutomation.BranchSettings;
using GitAutomation.GitService;
using GitAutomation.Repository;
using GitAutomation.Work;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Orchestration.Actions
{
    public delegate Task<bool> AttemptMergeDelegate(string upstreamBranch, string targetBranch, string message);

    public struct IntegrationBranchResult
    {
        public bool AddedNewIntegrationBranches;
        public bool HadPullRequest;
    }

    class IntegrateBranchesOrchestration
    {
        private readonly IUnitOfWorkFactory workFactory;
        private readonly IRepositoryOrchestration orchestration;
        private readonly IIntegrationNamingMediator integrationNaming;
        private readonly IBranchSettings settings;
        private readonly IGitServiceApi gitServiceApi;

        struct PossibleConflictingBranches
        {
            public string BranchA;
            public string BranchB;

            public ConflictingBranches? ConflictWhenSuccess;
        }

        struct ConflictingBranches
        {
            public string BranchA;
            public string BranchB;
        }

        public IntegrateBranchesOrchestration(IGitServiceApi gitServiceApi, IUnitOfWorkFactory workFactory, IRepositoryOrchestration orchestration, IIntegrationNamingMediator integrationNaming, IBranchSettings settings)
        {
            this.gitServiceApi = gitServiceApi;
            this.workFactory = workFactory;
            this.orchestration = orchestration;
            this.integrationNaming = integrationNaming;
            this.settings = settings;
        }

        public async Task<IntegrationBranchResult> FindAndCreateIntegrationBranches(BranchGroupCompleteData downstreamDetails, IEnumerable<string> initialUpstreamBranches, AttemptMergeDelegate doMerge)
        {
            // 1. Find branches that conflict
            // 2. Create integration branches for them
            // 3. Add the integration branch for ourselves


            // When finding branches that conflict, we should go to the earliest point of conflict... so we need to know full ancestry.
            // Except... we can start at the direct upstream, and if those conflict, then move up; a double breadth-first search.
            //
            // A & B -> C
            // D & E & F -> G
            // H & I -> J
            // C and G do not conflict.
            // C and J conflict.
            // G and J do not conflict.
            // A and J conflict.
            // B and J conflict.
            // A and H conflict.
            // B and H do not conflict.
            // A and I do not conflict.
            // B and I do not conflict.
            //
            // Two integration branches are needed: A-H and B-J. A-H is already found, so only B-J is created.
            // A-H and B-J are added to the downstream branch, if A-H was not already added to the downstream branch.

            var upstreamBranchListings = new Dictionary<string, ImmutableList<string>>();
            var hasOpenPullRequest = new Dictionary<string, bool>();
            var leafConflicts = new HashSet<ConflictingBranches>();
            var unflippedConflicts = new HashSet<ConflictingBranches>();
            // Remove from `middleConflicts` if we find a deeper one that conflicts
            var middleConflicts = new HashSet<ConflictingBranches>();
            var possibleConflicts = new Stack<PossibleConflictingBranches>(
                from branchA in initialUpstreamBranches
                from branchB in initialUpstreamBranches
                where branchA.CompareTo(branchB) < 0
                select new PossibleConflictingBranches { BranchA = branchA, BranchB = branchB, ConflictWhenSuccess = null }
            );

            Func<PossibleConflictingBranches, Task<bool>> digDeeper = async (possibleConflict) =>
            {
                ImmutableList<string> upstreamBranches = await GetUpstreamBranches(possibleConflict.BranchA, upstreamBranchListings);
                if (upstreamBranches.Count > 0)
                {
                    // go deeper on the left side
                    foreach (var possible in upstreamBranches)
                    {
                        possibleConflicts.Push(new PossibleConflictingBranches
                        {
                            BranchA = possible,
                            BranchB = possibleConflict.BranchB,
                            ConflictWhenSuccess = new ConflictingBranches { BranchA = possibleConflict.BranchA, BranchB = possibleConflict.BranchB }
                        });
                    }
                    return true;
                }
                return false;
            };

            var skippedDueToPullRequest = false;
            while (possibleConflicts.Count > 0)
            {
                var possibleConflict = possibleConflicts.Pop();
                if (leafConflicts.Contains(new ConflictingBranches { BranchA = possibleConflict.BranchA, BranchB = possibleConflict.BranchB }))
                {
                    continue;
                }
                if (await CachedHasOpenPullRequest(possibleConflict.BranchA, hasOpenPullRequest) || await CachedHasOpenPullRequest(possibleConflict.BranchB, hasOpenPullRequest))
                {
                    skippedDueToPullRequest = true;
                    continue;
                }
                var isSuccessfulMerge = await doMerge(possibleConflict.BranchA, possibleConflict.BranchB, "CONFLICT TEST; DO NOT PUSH");
                if (isSuccessfulMerge)
                {
                    // successful, not a conflict
                }
                else
                {
                    // there was a conflict
                    if (possibleConflict.ConflictWhenSuccess.HasValue && unflippedConflicts.Contains(possibleConflict.ConflictWhenSuccess.Value))
                    {
                        // so remove the intermediary
                        unflippedConflicts.Remove(possibleConflict.ConflictWhenSuccess.Value);
                    }
                    // ...and check deeper
                    var conflict = new ConflictingBranches { BranchA = possibleConflict.BranchA, BranchB = possibleConflict.BranchB };
                    unflippedConflicts.Add(conflict);
                    if (await digDeeper(possibleConflict))
                    {
                        // succeeded to dig deeper on branchA
                    }
                }
            }

            foreach (var conflict in unflippedConflicts)
            {
                if (await digDeeper(new PossibleConflictingBranches
                {
                    BranchA = conflict.BranchB,
                    BranchB = conflict.BranchA,
                    ConflictWhenSuccess = null,
                }))
                {
                    middleConflicts.Add(new ConflictingBranches { BranchA = conflict.BranchB, BranchB = conflict.BranchA });
                }
                else
                {
                    // Nothing deeper; this is our conflict
                    leafConflicts.Add(conflict);
                }
            }

            while (possibleConflicts.Count > 0)
            {
                var possibleConflict = possibleConflicts.Pop();
                if (leafConflicts.Contains(new ConflictingBranches { BranchA = possibleConflict.BranchA, BranchB = possibleConflict.BranchB }))
                {
                    continue;
                }
                if (await CachedHasOpenPullRequest(possibleConflict.BranchA, hasOpenPullRequest) || await CachedHasOpenPullRequest(possibleConflict.BranchB, hasOpenPullRequest))
                {
                    skippedDueToPullRequest = true;
                    continue;
                }
                var isSuccessfulMerge = await doMerge(possibleConflict.BranchA, possibleConflict.BranchB, "CONFLICT TEST; DO NOT PUSH");
                if (isSuccessfulMerge)
                {
                    // successful, not a conflict
                }
                else
                {
                    // there was a conflict
                    if (possibleConflict.ConflictWhenSuccess.HasValue && middleConflicts.Contains(possibleConflict.ConflictWhenSuccess.Value))
                    {
                        // so remove the intermediary
                        middleConflicts.Remove(possibleConflict.ConflictWhenSuccess.Value);
                    }
                    // ...and check deeper
                    var conflict = new ConflictingBranches { BranchA = possibleConflict.BranchA, BranchB = possibleConflict.BranchB };
                    if (await digDeeper(possibleConflict))
                    {
                        // succeeded to dig deeper on branchA
                        middleConflicts.Add(conflict);
                    }
                    else
                    {
                        // Nothing deeper; this is our conflict
                        leafConflicts.Add(conflict);
                    }
                }
            }

            var addedIntegrationBranch = false;
            using (var work = workFactory.CreateUnitOfWork())
            {
                foreach (var conflict in leafConflicts.Concat(middleConflicts))
                {
                    var integrationBranch = await settings.GetIntegrationBranch(conflict.BranchA, conflict.BranchB);
                    if (integrationBranch == null)
                    {
                        // TODO - integration branch naming
                        integrationBranch = await integrationNaming.GetIntegrationBranchName(conflict.BranchA, conflict.BranchB);
                        settings.CreateIntegrationBranch(conflict.BranchA, conflict.BranchB, integrationBranch, work);
                        orchestration.EnqueueAction(new MergeDownstreamAction(integrationBranch)).Subscribe();
                    }
                    if (!downstreamDetails.UpstreamBranchGroups.Any(b => b == integrationBranch))
                    {
                        addedIntegrationBranch = true;
                        settings.AddBranchPropagation(integrationBranch, downstreamDetails.GroupName, work);
                    }
                }
                await work.CommitAsync();
            }

            return new IntegrationBranchResult
            {
                AddedNewIntegrationBranches = addedIntegrationBranch,
                HadPullRequest = skippedDueToPullRequest,
            };
        }

        private async Task<bool> CachedHasOpenPullRequest(string branch, Dictionary<string, bool> hasOpenPullRequest)
        {
            if (!hasOpenPullRequest.ContainsKey(branch))
            {
                hasOpenPullRequest[branch] = await gitServiceApi.HasOpenPullRequest(targetBranch: branch);
            }
            return hasOpenPullRequest[branch];
        }

        private async Task<ImmutableList<string>> GetUpstreamBranches(string branch, Dictionary<string, ImmutableList<string>> upstreamBranchListings)
        {
            if (!upstreamBranchListings.ContainsKey(branch))
            {
                // FIXME - this should return the latest of each branch group instead
                upstreamBranchListings[branch] = (
                    from b in (await settings.GetUpstreamBranches(branch).FirstOrDefaultAsync())
                    select b.GroupName
                ).ToImmutableList();
            }
            return upstreamBranchListings[branch];
        }


    }
}
