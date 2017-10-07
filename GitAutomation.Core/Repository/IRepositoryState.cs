using GitAutomation.Processes;
using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitAutomation.Repository
{
    public interface IRepositoryState
    {
        IObservable<OutputMessage> DeleteBranch(string branchName);
        IObservable<OutputMessage> DeleteRepository();
        IObservable<OutputMessage> CheckForUpdates();
        
        IObservable<ImmutableList<GitRef>> RemoteBranches();
        IObservable<ImmutableList<GitRef>> DetectUpstream(string branchName);
        IObservable<string> MergeBaseBetween(string branchName1, string branchName2);
        void NotifyPushedRemoteBranch(string downstreamBranch);
    }
}