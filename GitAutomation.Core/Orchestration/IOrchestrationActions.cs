using System;
using GitAutomation.Processes;
using System.Collections.Generic;
using GitAutomation.Repository;

namespace GitAutomation.Orchestration
{
    public interface IOrchestrationActions
    {
        IObservable<IRepositoryActionEntry> CheckForUpdates();
        IObservable<IRepositoryActionEntry> DeleteBranch(string branchName, DeleteBranchMode mode);
        IObservable<IRepositoryActionEntry> DeleteRepository();
        IObservable<IRepositoryActionEntry> CheckDownstreamMerges(string downstreamBranch);
        IObservable<IRepositoryActionEntry> ReleaseToServiceLine(string releaseCandidateBranch, string serviceLineBranch, string tagName, bool autoConsolidate);
        IObservable<IRepositoryActionEntry> ConsolidateMerged(IEnumerable<string> originalBranches, string newBaseBranch);
        IObservable<IRepositoryActionEntry> Update();
    }
}