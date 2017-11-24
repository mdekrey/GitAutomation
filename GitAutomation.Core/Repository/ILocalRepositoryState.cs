using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Repository
{
    public interface ILocalRepositoryState
    {
        IObservable<ImmutableList<GitRef>> RemoteBranches();
        Task<ImmutableList<GitRef>> DetectUpstream(string branchName, bool allowSame);
        Task<string> MergeBaseBetweenCommits(string commit1, string commit2);
        Task<string> MergeBaseBetween(string branchName1, string branchName2);

        /// <summary>
        /// Dump internal cache and fetch from remote
        /// </summary>
        void RefreshAll();
        /// <summary>
        /// Informs the local repository that a branch was known to update to a specific revision
        /// </summary>
        /// <param name="branchName">The branch that was updated. Should not be null.</param>
        /// <param name="revision">The new revision. If null, the branch was deleted.</param>
        void BranchUpdated(string branchName, string revision);

        IGitCli Cli { get; }
    }
}
