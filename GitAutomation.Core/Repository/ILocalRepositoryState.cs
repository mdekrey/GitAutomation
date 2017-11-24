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
        /// Informs the local repository that it should fetch from the remote. If a specificRef is provided, fetch only that ref.
        /// </summary>
        /// <param name="specificRef">If provided, the ref to fetch.</param>
        void ShouldFetch(string specificRef = null);
        /// <summary>
        /// Informs the local repository that a remote branch no longer exists
        /// </summary>
        /// <param name="remoteBranch">The name of the remote branch that was removed</param>
        void NotifyRemoved(string remoteBranch);

        IGitCli Cli { get; }
    }
}
