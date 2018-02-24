using GitAutomation.Processes;
using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitAutomation.Orchestration;

namespace GitAutomation.Repository
{
    public interface IRemoteRepositoryState
    {
        
        IObservable<ImmutableList<GitRef>> RemoteBranches();
        Task<ImmutableList<GitRef>> DetectUpstream(string branchName, bool allowSame);
        Task<string> MergeBaseBetweenCommits(string commit1, string commit2);
        Task<string> MergeBaseBetween(string branchName1, string branchName2);

        /// <summary>
        /// Flag a given gitref as being "bad" until it is updated.
        /// </summary>
        /// <param name="target">The target git ref</param>
        void FlagBadGitRef(GitRef target, string reasonCode, DateTimeOffset? timestamp = null);
        Task<BadBranchInfo> GetBadBranchInfo(string branchName);

        Task<bool?> CanMerge(string branchNameA, string branchNameB);
        Task MarkCanMerge(string branchNameA, string branchNameB, bool canMerge);

        /// <summary>
        /// Dump internal cache and fetch from remote
        /// </summary>
        void RefreshAll();
        /// <summary>
        /// Informs the local repository that a branch was known to update to a specific revision
        /// </summary>
        /// <param name="branchName">The branch that was updated. Should not be null.</param>
        /// <param name="revision">The new revision. If null, the branch was deleted.</param>
        /// <param name="beforeRef">The old revision. If null, the branch was created. If this doesn't match what we have locally, something is out of date; we need to do a full refresh.</param>
        Task BranchUpdated(string branchName, string revision, string beforeRef);
        
    }
}