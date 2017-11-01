using GitAutomation.Processes;
using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitAutomation.Repository
{
    public interface IRepositoryState
    {
        IObservable<OutputMessage> DeleteBranch(string branchName, DeleteBranchMode mode);
        IObservable<OutputMessage> DeleteRepository();
        IObservable<OutputMessage> CheckForUpdates();
        
        IObservable<ImmutableList<GitRef>> RemoteBranches();
        IObservable<ImmutableList<GitRef>> DetectUpstream(string branchName, bool allowSame);
        Task<string> MergeBaseBetweenCommits(string commit1, string commit2);
        IObservable<string> MergeBaseBetween(string branchName1, string branchName2);
        void NotifyPushedRemoteBranch(string downstreamBranch);
    }
}