using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using GitAutomation.BranchSettings;
using GitAutomation.Repository;
using System.Reactive.Linq;
using System.Linq;
using GitAutomation.Work;

namespace GitAutomation
{
    class RepositoryMediator : IRepositoryMediator
    {
        private readonly IRepositoryState repositoryState;
        private readonly IBranchSettings branchSettings;
        private readonly IBranchIterationMediator branchIteration;

        public RepositoryMediator(IRepositoryState repositoryState, IBranchSettings branchSettings, IBranchIterationMediator branchIteration)
        {
            this.repositoryState = repositoryState;
            this.branchSettings = branchSettings;
            this.branchIteration = branchIteration;
        }

        public IObservable<ImmutableList<BranchBasicDetails>> AllBranches()
        {
            return (
                branchSettings.GetConfiguredBranches()
                .CombineLatest(
                    repositoryState.RemoteBranches(),
                    (first, second) =>
                        GroupBranches(first, second , branchName => new BranchBasicDetails { BranchName = branchName })
                            .OrderBy(a => a.BranchName)
                            .ToImmutableList()
                )
            );
        }

        public IObservable<ImmutableList<BranchHierarchyDetails>> AllBranchesHierarchy()
        {
            return (
                // TODO - should be more efficient with SQL here.
                branchSettings.GetAllDownstreamBranches()
                    .SelectMany(allBranches =>
                        allBranches.ToObservable().SelectMany(async branch => new BranchHierarchyDetails(branch)
                        {
                            DownstreamBranches = (await branchSettings.GetDownstreamBranches(branch.BranchName).FirstOrDefaultAsync()).Select(b => b.BranchName).ToImmutableList()
                        }).ToArray()
                    )
                    .Select(branches => branches.ToImmutableList())
                .CombineLatest(
                    repositoryState.RemoteBranches(),
                    (first, second) =>
                        GroupBranches(first, second , branchName => new BranchHierarchyDetails { BranchName = branchName })
                            .OrderBy(a => a.BranchName)
                            .ToImmutableList()
                )
            ).FirstAsync();
        }

        public void ConsolidateBranches(IEnumerable<string> branchesToRemove, string targetBranch, IUnitOfWork unitOfWork)
        {
            this.branchSettings.ConsolidateBranches(branchesToRemove, targetBranch, unitOfWork);
        }

        public IObservable<ImmutableList<string>> DetectShallowUpstream(string branchName)
        {
            return repositoryState.DetectUpstream(branchName)
                .CombineLatest(branchSettings.GetConfiguredBranches(), async (allUpstream, configured) =>
                {

                    for (var i = 0; i < allUpstream.Count - 1; i++)
                    {
                        var upstream = allUpstream[i];
                        var isConfigured = configured.Any(branch => branch.BranchName == upstream);
                        var furtherUpstream = await branchSettings.GetAllUpstreamBranches(upstream).FirstOrDefaultAsync();
                        allUpstream = allUpstream.Except(furtherUpstream.Select(b => b.BranchName)).ToImmutableList();
                    }

                    // TODO - this could be much smarter
                    for (var i = 0; i < allUpstream.Count - 1; i++)
                    {
                        var upstream = allUpstream[i];
                        var furtherUpstream = await repositoryState.DetectUpstream(upstream).FirstOrDefaultAsync();
                        allUpstream = allUpstream.Except(furtherUpstream).ToImmutableList();
                    }

                    return allUpstream;

                }).Switch();
        }

        public IObservable<BranchDetails> GetBranchDetails(string branchName)
        {
            return branchSettings.GetBranchDetails(branchName)
                .WithLatestFrom(this.repositoryState.RemoteBranches(),
                    (branchDetails, remoteBranches) =>
                    {
                        return new BranchDetails(branchDetails)
                        {
                            BranchNames = remoteBranches.Where(remoteBranch => branchIteration.IsBranchIteration(branchDetails.BranchName, remoteBranch)).ToImmutableList()
                        };
                    });
        }

        public IObservable<string> GetNextCandidateBranch(BranchDetails details, bool shouldMutate)
        {
            return (
                from remoteBranches in this.repositoryState.RemoteBranches()
                select branchIteration.GetNextBranchNameIteration(
                    details.BranchName,
                    from remoteBranch in remoteBranches
                    where this.branchIteration.IsBranchIteration(details.BranchName, remoteBranch)
                    select remoteBranch
                )
            ).Switch();
        }

        public IObservable<string> LatestBranchName(BranchBasicDetails details)
        {
            return (
                from remoteBranches in this.repositoryState.RemoteBranches()
                select branchIteration.GetLatestBranchNameIteration(
                    details.BranchName,
                    from remoteBranch in remoteBranches
                    where this.branchIteration.IsBranchIteration(details.BranchName, remoteBranch)
                    select remoteBranch
                )
            );
        }

        private IEnumerable<T> GroupBranches<T>(ImmutableList<T> settings, string[] actualBranches, Func<string, T> factory)
            where T : BranchBasicDetails
        {
            var nonconfiguredBranches = new HashSet<string>();
            var configuredBranches = settings.ToDictionary(b => b.BranchName);
            foreach (var actualBranch in actualBranches) {
                var configured = false;
                foreach (var configuredBranch in configuredBranches.Values)
                {
                    if (branchIteration.IsBranchIteration(configuredBranch.BranchName, actualBranch))
                    {
                        configuredBranch.BranchNames = configuredBranch.BranchNames?.Add(actualBranch) ?? Enumerable.Repeat(actualBranch, 1).ToImmutableList();
                        configured = true;
                        break;
                    }
                }
                if (!configured)
                {
                    nonconfiguredBranches.Add(actualBranch);
                }
            }
            return configuredBranches.Values
                .Concat(nonconfiguredBranches.Select(factory));
        }
    }
}
