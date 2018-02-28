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

    public struct LatestBranchGroup
    {
        public string GroupName;
        public string LatestBranchName;

        internal int CompareTo(LatestBranchGroup other)
        {
            return GroupName.CompareTo(other.GroupName);
        }

        public override bool Equals(object obj)
        {
            //
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (LatestBranchGroup)obj;

            return other.GroupName == GroupName;
        }

        public override int GetHashCode()
        {
            return GroupName.GetHashCode();
        }
    }

    public struct ConflictingBranches
    {
        public LatestBranchGroup BranchA;
        public LatestBranchGroup BranchB;

        public ConflictingBranches Normalize()
        {
            if (BranchA.GroupName.CompareTo(BranchB.GroupName) < 0)
            {
                return this;
            }
            return new ConflictingBranches
            {
                BranchA = BranchB,
                BranchB = BranchA,
            };
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            var other = (ConflictingBranches)obj;

            return other.BranchA.Equals(BranchA) && other.BranchB.Equals(BranchB);
        }

        public override int GetHashCode()
        {
            return BranchA.GetHashCode() ^ BranchB.GetHashCode();
        }
    }

    public struct IntegrationBranchResult
    {
        public bool Resolved;
        public bool PendingUpdates;
        public IEnumerable<ConflictingBranches> Conflicts;
        public IEnumerable<string> AddedBranches;

        public bool AddedNewIntegrationBranches => AddedBranches?.Any() ?? false;

        internal bool NeedsPullRequest()
        {
            return Resolved == false && AddedNewIntegrationBranches == false && PendingUpdates == false;
        }
    }

    class IntegrateBranchesOrchestration
    {
        private readonly IUnitOfWorkFactory workFactory;
        private readonly IRepositoryOrchestration orchestration;
        private readonly IIntegrationNamingMediator integrationNaming;
        private readonly IBranchSettings settings;
        private readonly IRepositoryMediator repository;
        private readonly IBranchIterationMediator branchIteration;
        private readonly IGitServiceApi gitServiceApi;

        struct PossibleConflictingBranches
        {
            public LatestBranchGroup BranchA;
            public LatestBranchGroup BranchB;

            public ConflictingBranches? ConflictWhenSuccess;
        }

        public IntegrateBranchesOrchestration(IGitServiceApi gitServiceApi, IUnitOfWorkFactory workFactory, IRepositoryOrchestration orchestration, IIntegrationNamingMediator integrationNaming, IBranchSettings settings, IRepositoryMediator repository, IBranchIterationMediator branchIteration)
        {
            this.gitServiceApi = gitServiceApi;
            this.workFactory = workFactory;
            this.orchestration = orchestration;
            this.integrationNaming = integrationNaming;
            this.settings = settings;
            this.repository = repository;
            this.branchIteration = branchIteration;
        }

        public async Task<IntegrationBranchResult> FindAndCreateIntegrationBranches(BranchGroupCompleteData downstreamDetails, IEnumerable<string> initialUpstreamBranchGroups, AttemptMergeDelegate doMerge)
        {
            // 1. Find branches that conflict
            // 2. Create integration branches for them
            // 3. Add the integration branch for ourselves

            foreach (var upstream in initialUpstreamBranchGroups)
            {
                if (await repository.IsBadBranch(upstream))
                {
                    // A branch that was directly upstream had updates pending
                    return new IntegrationBranchResult { PendingUpdates = true };
                }
            }
            
            var result = await FindConflicts(downstreamDetails.GroupName, initialUpstreamBranchGroups, doMerge);
            if (result.PendingUpdates)
            {
                return result;
            }

            var newIntegrationBranches = new List<string>();
            using (var work = workFactory.CreateUnitOfWork())
            {
                foreach (var conflict in result.Conflicts)
                {
                    // I need an integration branch!
                    var integrationBranch = await settings.FindIntegrationBranchForConflict(conflict.BranchA.GroupName, conflict.BranchB.GroupName, downstreamDetails.UpstreamBranchGroups);
                    if (downstreamDetails.UpstreamBranchGroups.Contains(integrationBranch))
                    {
                        // You already have one! - Maz Kanata
                        continue;
                    }
                    var originalIntegrationBranch = integrationBranch;
                    if (integrationBranch == null)
                    {
                        if (conflict.BranchA.GroupName == downstreamDetails.GroupName || conflict.BranchB.GroupName == downstreamDetails.GroupName)
                        {
                            continue;
                        }
                        integrationBranch = await integrationNaming.GetIntegrationBranchName(conflict.BranchA.GroupName, conflict.BranchB.GroupName);
                        settings.CreateIntegrationBranch(conflict.BranchA.GroupName, conflict.BranchB.GroupName, integrationBranch, work);
#pragma warning disable CS4014
                        orchestration.EnqueueAction(new MergeDownstreamAction(integrationBranch));
#pragma warning restore
                    }
                    if (originalIntegrationBranch != null && (conflict.BranchA.GroupName == downstreamDetails.GroupName || conflict.BranchB.GroupName == downstreamDetails.GroupName))
                    {
                        // There was an integration branch with the current branch and an upstream branch. Flip it around and consolidate!
                        newIntegrationBranches.Add(integrationBranch);
                        settings.AddBranchPropagation(integrationBranch, downstreamDetails.GroupName, work);
                        settings.RemoveBranchPropagation(downstreamDetails.GroupName, integrationBranch, work);

#pragma warning disable CS4014
                        orchestration.EnqueueAction(new MergeDownstreamAction(downstreamDetails.GroupName));
                        orchestration.EnqueueAction(new ConsolidateMergedAction(integrationBranch, downstreamDetails.GroupName));
#pragma warning restore
                    }
                    else if (!downstreamDetails.UpstreamBranchGroups.Any(b => b == integrationBranch))
                    {
                        newIntegrationBranches.Add(integrationBranch);
                        settings.AddBranchPropagation(integrationBranch, downstreamDetails.GroupName, work);
                    }
                }
                await work.CommitAsync();
            }

            return new IntegrationBranchResult
            {
                Conflicts = result.Conflicts,
                AddedBranches = newIntegrationBranches,
            };
        }

        public async Task<IntegrationBranchResult> FindConflicts(string targetBranch, IEnumerable<string> _, AttemptMergeDelegate doMerge)
        {

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

            var remoteBranches = await repository.GetAllBranchRefs().Select(branches => branches.Select(branch => branch.Name).ToImmutableList()).FirstOrDefaultAsync();
            Func<string, LatestBranchGroup> groupToLatest = group => new LatestBranchGroup
            {
                GroupName = group,
                LatestBranchName = branchIteration.GetLatestBranchNameIteration(group, remoteBranches)
            };


            var upstreamBranchListings = new Dictionary<string, ImmutableList<string>>();
            var initialUpstreamBranchGroups = await GetUpstreamBranches(targetBranch, upstreamBranchListings);

            await GetUpstreamBranches(targetBranch, upstreamBranchListings);
            var leafConflicts = new HashSet<ConflictingBranches>();
            var unflippedConflicts = new HashSet<ConflictingBranches>();
            // Remove from `middleConflicts` if we find a deeper one that conflicts
            var middleConflicts = new HashSet<ConflictingBranches>();
            var target = groupToLatest(targetBranch);

            var possibleConflicts = new Stack<PossibleConflictingBranches>(
                (
                    from branchA in initialUpstreamBranchGroups.Select(groupToLatest)
                    from branchB in initialUpstreamBranchGroups.Select(groupToLatest)
                    where branchA.CompareTo(branchB) < 0
                    select new PossibleConflictingBranches { BranchA = branchA, BranchB = branchB, ConflictWhenSuccess = null }
                ).Concat(
                    from branch in initialUpstreamBranchGroups.Select(groupToLatest)
                    where target.LatestBranchName != null
                    select new PossibleConflictingBranches { BranchA = branch, BranchB = target, ConflictWhenSuccess = null }
                )
            );

            Func<PossibleConflictingBranches, Task<bool>> digDeeper = async (possibleConflict) =>
            {
                var upstreamBranches = (await GetUpstreamBranches(possibleConflict.BranchA.GroupName, upstreamBranchListings)).Select(groupToLatest).ToImmutableList();
                if (upstreamBranches.Count > 0)
                {
                    // go deeper on the left side
                    foreach (var possible in upstreamBranches.Where(b => b.GroupName != possibleConflict.BranchB.GroupName))
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

            while (possibleConflicts.Count > 0)
            {
                if (possibleConflicts.Any(p => p.BranchA.LatestBranchName == null || p.BranchB.LatestBranchName == null))
                {
                    return new IntegrationBranchResult
                    {
                        PendingUpdates = true,
                    };
                }
                var possibleConflict = possibleConflicts.Pop();

                if (await repository.IsBadBranch(possibleConflict.BranchA.LatestBranchName) || await repository.IsBadBranch(possibleConflict.BranchB.LatestBranchName))
                {
                    // At least one bad branch was found
                    return new IntegrationBranchResult
                    {
                        PendingUpdates = true,
                    };
                }
                if (leafConflicts.Contains(new ConflictingBranches { BranchA = possibleConflict.BranchA, BranchB = possibleConflict.BranchB }))
                {
                    continue;
                }
                var isSuccessfulMerge = await repository.CanMerge(possibleConflict.BranchA.LatestBranchName, possibleConflict.BranchB.LatestBranchName)
                    ?? await doMerge(possibleConflict.BranchA.LatestBranchName, possibleConflict.BranchB.LatestBranchName, "CONFLICT TEST; DO NOT PUSH");
                if (isSuccessfulMerge)
                {
                    // successful, not a conflict
                    await repository.MarkCanMerge(possibleConflict.BranchA.LatestBranchName, possibleConflict.BranchB.LatestBranchName, true);
                }
                else
                {
                    await repository.MarkCanMerge(possibleConflict.BranchA.LatestBranchName, possibleConflict.BranchB.LatestBranchName, false);

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
                if (await repository.IsBadBranch(possibleConflict.BranchA.LatestBranchName) || await repository.IsBadBranch(possibleConflict.BranchB.LatestBranchName))
                {
                    // At least one bad branch was found
                    return new IntegrationBranchResult
                    {
                        PendingUpdates = true,
                    };
                }
                if (leafConflicts.Contains(new ConflictingBranches { BranchA = possibleConflict.BranchA, BranchB = possibleConflict.BranchB }))
                {
                    continue;
                }
                var isSuccessfulMerge = await doMerge(possibleConflict.BranchA.LatestBranchName, possibleConflict.BranchB.LatestBranchName, "CONFLICT TEST; DO NOT PUSH");
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

            return new IntegrationBranchResult
            {
                Conflicts = leafConflicts.Concat(middleConflicts).Select(c => c.Normalize()).Distinct(),
            };
        }

        private async Task<ImmutableList<string>> GetUpstreamBranches(string branch, Dictionary<string, ImmutableList<string>> upstreamBranchListings)
        {
            if (!upstreamBranchListings.ContainsKey(branch))
            {
                var result = (
                    from b in (await settings.GetUpstreamBranches(branch).FirstOrDefaultAsync())
                    select b.GroupName
                ).ToImmutableList();
                var removed = new HashSet<string>();
                foreach (var entry in result.ToArray())
                {
                    if (removed.Contains(entry))
                    {
                        // already found this as an exception
                        continue;
                    }
                    foreach (var newEntry in (await settings.GetAllUpstreamBranches(entry).FirstOrDefaultAsync()).Select(b => b.GroupName))
                    {
                        removed.Add(newEntry);
                    }
                }
                upstreamBranchListings[branch] = result.Except(removed).ToImmutableList();
            }
            return upstreamBranchListings[branch];
        }

        public async Task<IntegrationBranchResult> FindSingleIntegrationBranch(BranchGroupCompleteData details, string groupName, AttemptMergeDelegate doMerge)
        {
            var groups = new[] { details.GroupName, groupName }.OrderBy(g => g).ToArray();
            var integrationBranch = await settings.FindIntegrationBranchForConflict(groups[0], groups[1], ImmutableList<string>.Empty);
            if (integrationBranch == null)
            {
                return new IntegrationBranchResult
                {
                    AddedBranches = Enumerable.Empty<string>(),
                };
            }

            await doMerge(integrationBranch, details.LatestBranchName, $"Auto-merge branch '{integrationBranch}'");

            using (var work = workFactory.CreateUnitOfWork())
            {
                settings.AddBranchPropagation(integrationBranch, details.GroupName, work);
                settings.RemoveBranchPropagation(details.GroupName, integrationBranch, work);
                settings.RemoveBranchPropagation(groupName, integrationBranch, work);
                await work.CommitAsync();
            }

#pragma warning disable CS4014
            orchestration.EnqueueAction(new ConsolidateMergedAction(integrationBranch, details.GroupName));
#pragma warning restore

            return new IntegrationBranchResult
            {
                Resolved = true,
                AddedBranches = new[] { integrationBranch },
            };
        }
    }
}
