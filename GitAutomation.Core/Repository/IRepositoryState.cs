using GitAutomation.Processes;
using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitAutomation.Orchestration;

namespace GitAutomation.Repository
{
    public interface IRepositoryState
    {
        
        IObservable<ImmutableList<GitRef>> RemoteBranches();
        IObservable<ImmutableList<GitRef>> DetectUpstream(string branchName, bool allowSame);
        Task<string> MergeBaseBetweenCommits(string commit1, string commit2);
        IObservable<string> MergeBaseBetween(string branchName1, string branchName2);

        // TODO - this should provide more information
        void NotifyUpdated();
        // TODO - revise this so that it either is an "action" or it at least has the commit hash that it was pushed to
        void NotifyPushedRemoteBranch(string downstreamBranch);
    }
}