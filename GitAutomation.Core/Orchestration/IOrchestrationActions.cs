using System;
using GitAutomation.Processes;
using System.Collections.Generic;

namespace GitAutomation.Orchestration
{
    public interface IOrchestrationActions
    {
        IObservable<IRepositoryActionEntry> CheckDownstreamMerges(string downstreamBranch);
        IObservable<IRepositoryActionEntry> ReleaseToServiceLine(string releaseCandidateBranch, string serviceLineBranch, string tagName, bool autoConsolidate);
        IObservable<IRepositoryActionEntry> ConsolidateMerged(IEnumerable<string> originalBranches, string newBaseBranch);
        IObservable<IRepositoryActionEntry> Update();
    }
}