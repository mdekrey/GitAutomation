﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using GitAutomation.BranchSettings;
using GitAutomation.Repository;
using System.Reactive.Linq;
using System.Linq;
using GitAutomation.Work;
using GitAutomation.GitService;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Reactive.Threading.Tasks;
using GitAutomation.Orchestration;

namespace GitAutomation
{
    class RepositoryMediator : IRepositoryMediator
    {
        private readonly IRemoteRepositoryState repositoryState;
        private readonly IBranchSettings branchSettings;
        private readonly IBranchIterationMediator branchIteration;
        private readonly IGitServiceApi gitApi;
        private readonly IOrchestrationActions actions;

        public RepositoryMediator(IRemoteRepositoryState repositoryState, IBranchSettings branchSettings, IBranchIterationMediator branchIteration, IGitServiceApi gitApi, IOrchestrationActions actions)
        {
            this.repositoryState = repositoryState;
            this.branchSettings = branchSettings;
            this.branchIteration = branchIteration;
            this.gitApi = gitApi;
            this.actions = actions;
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

        public IObservable<ImmutableList<BranchGroup>> GetConfiguredBranchGroups()
        {
            return branchSettings.GetConfiguredBranches();
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

        public async Task<ImmutableList<string>> DetectUpstream(string actualBranchName)
        {
            var results = await repositoryState.DetectUpstream(actualBranchName, true);
            return results.Select(r => r.Name).ToImmutableList();
        }

        public async Task<ImmutableList<string>> DetectShallowUpstream(string branchName, bool asGroup)
        {
            var remoteBranchesTask = repositoryState.RemoteBranches().FirstOrDefaultAsync().ToTask();
            var configuredBranchesTask = branchSettings.GetConfiguredBranches().FirstOrDefaultAsync().ToTask();
            await Task.WhenAll(remoteBranchesTask, configuredBranchesTask);
            var allRemotes = remoteBranchesTask.Result;
            var configured = configuredBranchesTask.Result;

            var actualBranchName = asGroup
                ? branchIteration.GetLatestBranchNameIteration(branchName, allRemotes.Select(b => b.Name))
                : branchName;
            var allUpstream = await repositoryState.DetectUpstream(actualBranchName, false);
                
			return await PruneUpstream(allRemotes.Find(remote => remote.Name == branchName), allUpstream, configured, allRemotes);
		}

        private async System.Threading.Tasks.Task<ImmutableList<string>> PruneUpstream(GitRef original, ImmutableList<GitRef> allUpstream, ImmutableList<BranchGroup> configured, ImmutableList<GitRef> allRemotes)
        {
            var configuredLatest = configured.ToDictionary(branch => branch.GroupName, branch => branchIteration.GetLatestBranchNameIteration(branch.GroupName, allRemotes.Select(b => b.Name)));
            allUpstream = allUpstream.Where(maybeHasNewer =>
                !configured.Any(c => branchIteration.IsBranchIteration(c.GroupName, maybeHasNewer.Name) && maybeHasNewer.Name != configuredLatest[c.GroupName])
            ).ToImmutableList();

            for (var i = 0; i < allUpstream.Count; i++)
            {
                var upstream = allUpstream[i];
                var isConfigured = configuredLatest.Values.Contains(upstream.Name);
                var furtherUpstream = (from branchGroup in (await branchSettings.GetAllUpstreamBranches(upstream.Name).FirstOrDefaultAsync())
                                       let latest = configuredLatest[branchGroup.GroupName]
                                       where allRemotes.Find(b => b.Name == latest).Commit != upstream.Commit
                                       select latest)
                    .ToImmutableHashSet();
                var oldLength = allUpstream.Count;
                allUpstream = allUpstream.Where(maybeMatch => !furtherUpstream.Contains(maybeMatch.Name)).ToImmutableList();
                if (oldLength != allUpstream.Count)
                {
                    i = -1;
                }
            }

            // TODO - this could be much smarter
            for (var i = 0; i < allUpstream.Count; i++)
            {
                var upstream = allUpstream[i];
                var furtherUpstream = await repositoryState.DetectUpstream(upstream.Name, false);
                if (allUpstream.Intersect(furtherUpstream).Any())
                {
                    allUpstream = allUpstream.Except(furtherUpstream).ToImmutableList();
                    i = -1;
                }
            }

            return allUpstream.Select(b => b.Name).ToImmutableList();
        }

        public IObservable<ImmutableList<string>> DetectShallowUpstreamServiceLines(string branchName)
        {
            return branchSettings.GetConfiguredBranches().Select(branches => branches.Find(branch => branchIteration.IsBranchIteration(branch.GroupName, branchName)))
                .SelectMany(branch => branchSettings.GetAllUpstreamBranches(branch.GroupName))
                .CombineLatest(branchSettings.GetConfiguredBranches(), repositoryState.RemoteBranches(), (allUpstreamBranchDetails, configured, allRemotes) =>
                {
                    var allUpstream = allUpstreamBranchDetails.Where(b => b.BranchType == BranchGroupType.ServiceLine).Select(b => b.GroupName).ToImmutableList();
                    return PruneUpstream(
                        allRemotes.Find(remote => remote.Name == branchName),
                        allUpstream
                            .Select(upstream => branchIteration.GetLatestBranchNameIteration(upstream, allRemotes.Select(r => r.Name)))
                            .Select(branch => allRemotes.First(b => b.Name == branch))
                            .ToImmutableList(), 
                        configured, 
                        allRemotes
                    );
                }).Switch();
        }

        public IObservable<ImmutableList<PullRequest>> GetUpstreamPullRequests(string branchName)
        {
            return repositoryState.RemoteBranches()
                .Select(remoteBranches =>
                {
                    var branch = branchIteration.GetLatestBranchNameIteration(branchName, remoteBranches.Select(b => b.Name).Where(candidate => branchIteration.IsBranchIteration(branchName, candidate)));
                    return gitApi.GetPullRequests(state: PullRequestState.Open, targetBranch: branch, includeReviews: true);
                }).Switch();
        }

        public IObservable<BranchGroupCompleteData> GetBranchDetails(string branchName)
        {
            return this.repositoryState.RemoteBranches()
                .Select(remoteBranches =>
                    {
                        return branchSettings.GetConfiguredBranches()
                            .Select(branches => branches.FirstOrDefault(branch => branch.GroupName == branchName) 
                                             ?? branches.FirstOrDefault(branch => branchIteration.IsBranchIteration(branch.GroupName, branchName)))
                            .Select(branchBasicDetails => branchSettings.GetBranchDetails(branchBasicDetails?.GroupName ?? branchName))
                            .Switch()
                            .Select(branchDetails => AddRemoteBranchNames(branchDetails, remoteBranches));
                    }).Switch();
        }

        private BranchGroupCompleteData AddRemoteBranchNames(BranchGroupCompleteData branchDetails, ImmutableList<GitRef> remoteBranches)
        {
            var names = remoteBranches.Where(remoteBranch => branchIteration.IsBranchIteration(branchDetails.GroupName, remoteBranch.Name)).ToImmutableList();
            return new BranchGroupCompleteData(branchDetails)
            {
                Branches = names,
                LatestBranchName = names.Count == 0 ? null : branchIteration.GetLatestBranchNameIteration(branchDetails.GroupName, names.Select(b => b.Name)),
            };
        }

        public IObservable<string> GetNextCandidateBranch(BranchGroup details)
        {
            return (
                from remoteBranches in this.repositoryState.RemoteBranches()
                select branchIteration.GetNextBranchNameIteration(
                    details.GroupName,
                    from remoteBranch in remoteBranches
                    where this.branchIteration.IsBranchIteration(details.GroupName, remoteBranch.Name)
                    select remoteBranch.Name
                )
            ).Switch();
        }

        public IObservable<string> LatestBranchName(BranchGroup details)
        {
            return (
                from remoteBranches in this.repositoryState.RemoteBranches()
                select branchIteration.GetLatestBranchNameIteration(
                    details.GroupName,
                    from remoteBranch in remoteBranches
                    where this.branchIteration.IsBranchIteration(details.GroupName, remoteBranch.Name)
                    select remoteBranch.Name
                )
            );
        }

        public IObservable<ImmutableList<GitRef>> GetAllBranchRefs() =>
            repositoryState.RemoteBranches();

        public IObservable<string> GetBranchRef(string branchName) =>
            repositoryState.RemoteBranches()
                .Select(gitref => gitref.FirstOrDefault(gr => gr.Name == branchName).Commit);

        public async Task<bool> HasOutstandingCommits(string upstreamBranch, string downstreamBranch)
        {
            var mergeBaseTask = repositoryState.MergeBaseBetween(upstreamBranch, downstreamBranch);
            var showRefTask = GetBranchRef(upstreamBranch).FirstOrDefaultAsync().ToTask();
            await Task.WhenAll(mergeBaseTask, showRefTask);
            return mergeBaseTask.Result != showRefTask.Result;
        }

        private async Task<IEnumerable<BranchGroupCompleteData>> GroupBranches(ImmutableList<BranchGroupCompleteData> settings, ImmutableList<GitRef> actualBranches, Func<string, Task<BranchGroupCompleteData>> factory)
        {
            var nonconfiguredBranches = new HashSet<GitRef>();
            var configuredBranches = settings.ToDictionary(b => b.GroupName);
            var statuses = await gitApi.GetCommitStatuses(actualBranches.Select(b => b.Commit).ToImmutableList());
            foreach (var actualBranch in actualBranches) {
                var configured = false;
                foreach (var configuredBranch in configuredBranches.Values)
                {
                    if (branchIteration.IsBranchIteration(configuredBranch.GroupName, actualBranch.Name))
                    {
                        configuredBranch.Branches = configuredBranch.Branches?.Add(actualBranch) ?? Enumerable.Repeat(actualBranch, 1).ToImmutableList();
                        configuredBranch.Statuses = statuses[actualBranch.Commit];
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
                var result = await factory(branch.Name);
                result.Branches = result.Branches ?? Enumerable.Repeat(branch, 1).ToImmutableList();
                result.Statuses = ImmutableList<CommitStatus>.Empty;
                return result;
            }).ToArray();
            return configuredBranches.Values
                .Select(group =>
                {
                    group.Branches = group.Branches ?? ImmutableList<GitRef>.Empty;
                    return group;
                })
                .Concat(nonconfiguredBranchesResult);
        }

        public async Task BranchUpdated(string downstreamBranch, string newRef, string oldRef)
        {
            await repositoryState.BranchUpdated(downstreamBranch, newRef, oldRef);
        }

        public IObservable<ImmutableList<string>> RecommendNewGroups()
        {
            return branchSettings.GetConfiguredBranches()
                .CombineLatest(
                    repositoryState.RemoteBranches(),
                    (configured, allRemotes) =>
                    {
                        var remainingRemotes = 
                            from remote in allRemotes
                            where !configured.Any(current => branchIteration.IsBranchIteration(current.GroupName, remote.Name))
                            select remote;
                        return (
                            from remote in remainingRemotes
                            group remote.Name by branchIteration.GuessBranchIterationRoot(remote.Name) into groups
                            select groups.Key
                        ).ToImmutableList();
                    }
                );
        }

        public void CheckForUpdates()
        {
            actions.CheckForUpdates();
        }

        public void CheckForUpdatesOnBranch(string branchName)
        {
            actions.CheckForUpdatesOnBranch(branchName);
        }

        public void FlagBadGitRef(string branch, string commit, string reasonCode, DateTimeOffset? timestamp = null)
        {
            repositoryState.FlagBadGitRef(new GitRef { Name = branch, Commit = commit }, reasonCode, timestamp);
        }

        public Task<BadBranchInfo> GetBadBranchInfo(string branch)
        {
            return repositoryState.GetBadBranchInfo(branch);
        }

        public Task ResetBadBranchStatus(string branchName)
        {
            return repositoryState.ResetBadBranchStatus(branchName);
        }

        public Task<bool?> CanMerge(string branchNameA, string branchNameB)
        {
            return repositoryState.CanMerge(branchNameA, branchNameB);
        }
        public Task MarkCanMerge(string branchNameA, string branchNameB, bool canMerge)
        {
            return repositoryState.MarkCanMerge(branchNameA, branchNameB, canMerge);
        }

        public async Task<bool> AddAdditionalIntegrationBranches(BranchGroupCompleteData details, IUnitOfWork unitOfWork)
        {
            var branches = await branchSettings.GetIntegrationBranches(details.UpstreamBranchGroups);
            var toAdd = branches.Except(details.UpstreamBranchGroups).Except(new [] { details.GroupName }).ToImmutableList();
            if (toAdd.Any())
            {
                foreach (var upstreamBranch in toAdd)
                {
                    branchSettings.AddBranchPropagation(upstreamBranch, details.GroupName, unitOfWork);
                }
                return true;
            }
            return false;
        }

    }
}
