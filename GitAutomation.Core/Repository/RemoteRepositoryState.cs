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

        public RemoteRepositoryState(IEnumerable<ILocalRepositoryState> localRepositories)
        {
            this.localRepositories = localRepositories;
        }

        #region Updates
        
        public void RefreshAll()
        {
            foreach (var local in localRepositories)
            {
                local.RefreshAll();
            }
        }

        public void BranchUpdated(string branchName, string revision)
        {
            foreach (var local in localRepositories)
            {
                local.BranchUpdated(branchName, revision);
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

        #endregion

    }
}
