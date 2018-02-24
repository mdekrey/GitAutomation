using GitAutomation.Processes;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive;
using System.Collections.Immutable;
using GitAutomation.Orchestration.Actions;
using GitAutomation.Orchestration;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GitAutomation.Repository
{
    class RemoteRepositoryState : IRemoteRepositoryState
    {
        private readonly IEnumerable<ILocalRepositoryState> localRepositories;
        private readonly ConcurrentDictionary<string, BadBranchInfo> badBranchCommits = new ConcurrentDictionary<string, BadBranchInfo>();
        private readonly ConcurrentDictionary<Tuple<string, string>, bool> canMerge = new ConcurrentDictionary<Tuple<string, string>, bool>();

        public RemoteRepositoryState(IEnumerable<ILocalRepositoryState> localRepositories)
        {
            this.localRepositories = localRepositories;
        }
        
        public void RefreshAll()
        {
            foreach (var local in localRepositories)
            {
                local.RefreshAll();
            }
        }

        public async Task BranchUpdated(string branchName, string revision, string oldRevision)
        {
            var gitRef = (await RemoteBranches().FirstOrDefaultAsync()).Where(g => g.Name == branchName).FirstOrDefault();
            if (gitRef.Commit == oldRevision)
            {
                foreach (var local in localRepositories)
                {
                    local.BranchUpdated(branchName, revision);
                }
            }
            else
            {
                // Something's wrong _everywhere_ because the old one didn't match
                RefreshAll();
            }
        }

        public IObservable<ImmutableList<GitRef>> RemoteBranches()
        {
            return localRepositories.First().RemoteBranches();
        }

        public Task<ImmutableList<GitRef>> DetectUpstream(string branchName, bool allowSame)
        {
            return localRepositories.First().DetectUpstream(branchName, allowSame);
        }

        public Task<string> MergeBaseBetweenCommits(string commit1, string commit2)
        {
            return localRepositories.First().MergeBaseBetweenCommits(commit1, commit2);
        }

        public Task<string> MergeBaseBetween(string branchName1, string branchName2)
        {
            return localRepositories.First().MergeBaseBetween(branchName1, branchName2);
        }

        public void FlagBadGitRef(GitRef target, string reasonCode, DateTimeOffset? timestamp = null)
        {
            var info = new BadBranchInfo
            {
                Commit = target.Commit,
                ReasonCode = reasonCode,
                Timestamp = timestamp ?? DateTimeOffset.Now,
            };
            badBranchCommits.AddOrUpdate(target.Name, info, (_1, _2) => info);
        }

        public async Task<BadBranchInfo> GetBadBranchInfo(string branchName)
        {
            if (!badBranchCommits.TryGetValue(branchName, out var badInfo))
            {
                return null;
            }
            if (badInfo == null)
            {
                return null;
            }
            var branches = await RemoteBranches().Take(1);
            var branch = branches.FirstOrDefault(b => b.Name == branchName);
            if (branch.Commit != badInfo.Commit)
            {
                badBranchCommits.TryUpdate(branchName, null, badInfo);
                return null;
            }
            return badInfo;
        }

        private async Task<Tuple<string, string>> GetMergeTuple(string branchNameA, string branchNameB)
        {
            var branches = await RemoteBranches().Take(1);
            var commitA = branches.FirstOrDefault(b => b.Name == branchNameA).Commit;
            var commitB = branches.FirstOrDefault(b => b.Name == branchNameB).Commit;
            if (commitA == null || commitB == null)
            {
                return null;
            }
            return commitA.CompareTo(commitB) < 0
                ? Tuple.Create(commitA, commitB)
                : Tuple.Create(commitB, commitA);
        }

        public async Task<bool?> CanMerge(string branchNameA, string branchNameB)
        {
            var key = await GetMergeTuple(branchNameA, branchNameB);
            if (key == null || !canMerge.TryGetValue(key, out var canMergeResult))
            {
                return null;
            }

            return canMergeResult;
        }
        public async Task MarkCanMerge(string branchNameA, string branchNameB, bool canMerge)
        {
            var key = await GetMergeTuple(branchNameA, branchNameB);
            if (key != null)
            {
                this.canMerge.TryAdd(key, canMerge);
            }
        }
    }
}
