using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using GitAutomation.BranchSettings;
using GitAutomation.Repository;
using System.Reactive.Linq;
using System.Linq;
using GitAutomation.Work;
using System.Threading.Tasks;

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

        public IObservable<ImmutableList<BranchGroupCompleteData>> AllBranches()
        {
            return (
                branchSettings.GetConfiguredBranches().Select(e => e.Select(group => new BranchGroupCompleteData(group)).ToImmutableList())
                .CombineLatest(
                    repositoryState.RemoteBranches(),
                    (first, second) => new { first, second }
                )
                .SelectMany(async param =>
                        (await GroupBranches(param.first, param.second, ToDefaultBranchGroup))
                            .OrderBy(a => a.GroupName)
                            .ToImmutableList()
                )
            );
        }

        private Task<BranchGroupCompleteData> ToDefaultBranchGroup(string arg)
        {
            return Task.FromResult(new BranchGroupCompleteData { GroupName = arg });
        }

        public IObservable<ImmutableList<BranchGroupCompleteData>> AllBranchesHierarchy()
        {
            return (
                // TODO - should be more efficient with SQL here.
                branchSettings.GetAllDownstreamBranches()
                    .SelectMany(allBranches =>
                        allBranches.ToObservable().SelectMany(async branch => new BranchGroupCompleteData(branch)
                        {
                            DownstreamBranchGroups = (await branchSettings.GetDownstreamBranches(branch.GroupName).FirstOrDefaultAsync()).Select(b => b.GroupName).ToImmutableList()
                        }).ToArray()
                    )
                    .Select(branches => branches.ToImmutableList())
                .CombineLatest(
                    repositoryState.RemoteBranches(),
                    (first, second) => new { first, second }
                )
                .SelectMany(async param =>
                        (await GroupBranches(param.first, param.second, ToDefaultBranchGroup))
                            .OrderBy(a => a.GroupName)
                            .Select(a => new BranchGroupCompleteData(a)
                            {
                                DownstreamBranchGroups = a.DownstreamBranchGroups ?? ImmutableList<string>.Empty,
                            })
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

                    for (var i = 0; i < allUpstream.Count; i++)
                    {
                        var upstream = allUpstream[i];
                        var isConfigured = configured.Any(branch => branch.GroupName == upstream);
                        var furtherUpstream = await branchSettings.GetAllUpstreamBranches(upstream).FirstOrDefaultAsync();
                        allUpstream = allUpstream.Except(furtherUpstream.Select(b => b.GroupName)).ToImmutableList();
                    }

                    // TODO - this could be much smarter
                    for (var i = 0; i < allUpstream.Count; i++)
                    {
                        var upstream = allUpstream[i];
                        var furtherUpstream = await repositoryState.DetectUpstream(upstream).FirstOrDefaultAsync();
                        if (allUpstream.Intersect(furtherUpstream).Any())
                        {
                            allUpstream = allUpstream.Except(furtherUpstream).ToImmutableList();
                            i = -1;
                        }
                    }

                    return allUpstream;

                }).Switch();
        }

        public IObservable<BranchGroupCompleteData> GetBranchDetails(string branchName)
        {
            return this.repositoryState.RemoteBranches()
                .Select(remoteBranches =>
                    {
                        return branchSettings.GetConfiguredBranches()
                            .Select(branches => branches.FirstOrDefault(branch => branchIteration.IsBranchIteration(branch.GroupName, branchName)))
                            .Select(branchBasicDetails => branchSettings.GetBranchDetails(branchBasicDetails?.GroupName ?? branchName))
                            .Switch()
                            .Select(branchDetails => AddRemoteBranchNames(branchDetails, remoteBranches));
                    }).Switch();
        }

        private BranchGroupCompleteData AddRemoteBranchNames(BranchGroupCompleteData branchDetails, string[] remoteBranches)
        {
            return new BranchGroupCompleteData(branchDetails)
            {
                BranchNames = remoteBranches.Where(remoteBranch => branchIteration.IsBranchIteration(branchDetails.GroupName, remoteBranch)).ToImmutableList()
            };
        }

        public IObservable<string> GetNextCandidateBranch(BranchGroupDetails details, bool shouldMutate)
        {
            return (
                from remoteBranches in this.repositoryState.RemoteBranches()
                select branchIteration.GetNextBranchNameIteration(
                    details.GroupName,
                    from remoteBranch in remoteBranches
                    where this.branchIteration.IsBranchIteration(details.GroupName, remoteBranch)
                    select remoteBranch
                )
            ).Switch();
        }

        public IObservable<string> LatestBranchName(BranchGroupDetails details)
        {
            return (
                from remoteBranches in this.repositoryState.RemoteBranches()
                select branchIteration.GetLatestBranchNameIteration(
                    details.GroupName,
                    from remoteBranch in remoteBranches
                    where this.branchIteration.IsBranchIteration(details.GroupName, remoteBranch)
                    select remoteBranch
                )
            );
        }

        public IObservable<ImmutableList<GitRef>> GetAllBranchRefs() =>
            repositoryState.RemoteBranchesWithRefs();

        public IObservable<string> GetBranchRef(string branchName) =>
            repositoryState.RemoteBranchesWithRefs()
                .Select(gitref => gitref.Exists(gr => gr.Name == branchName) ? gitref.Find(gr => gr.Name == branchName).Commit : null);

        public IObservable<bool> HasOutstandingCommits(string upstreamBranch, string downstreamBranch)
        {
            return Observable.CombineLatest(
                repositoryState.MergeBaseBetween(upstreamBranch, downstreamBranch),
                GetBranchRef(upstreamBranch),
                (mergeBaseResult, showRefResult) => mergeBaseResult != showRefResult
            );
        }

        private async Task<IEnumerable<BranchGroupCompleteData>> GroupBranches(ImmutableList<BranchGroupCompleteData> settings, string[] actualBranches, Func<string, Task<BranchGroupCompleteData>> factory)
        {
            var nonconfiguredBranches = new HashSet<string>();
            var configuredBranches = settings.ToDictionary(b => b.GroupName);
            foreach (var actualBranch in actualBranches) {
                var configured = false;
                foreach (var configuredBranch in configuredBranches.Values)
                {
                    if (branchIteration.IsBranchIteration(configuredBranch.GroupName, actualBranch))
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
            var nonconfiguredBranchesResult = await nonconfiguredBranches.ToObservable().SelectMany(async branch =>
            {
                var result = await factory(branch);
                result.BranchNames = Enumerable.Repeat(branch, 1).ToImmutableList();
                return result;
            }).ToArray();
            return configuredBranches.Values
                .Concat(nonconfiguredBranchesResult);
        }

        public void NotifyPushedRemoteBranch(string downstreamBranch)
        {
            repositoryState.NotifyPushedRemoteBranch(downstreamBranch);
        }
    }
}
