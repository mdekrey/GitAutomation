using System;
using GitAutomation.Processes;
using System.Collections.Generic;

namespace GitAutomation.Orchestration
{
    public interface IOrchestrationActions
    {
        IObservable<OutputMessage> CheckDownstreamMerges(string downstreamBranch);
        IObservable<OutputMessage> ReleaseToServiceLine(string releaseCandidateBranch, string serviceLineBranch, string tagName);
        IObservable<OutputMessage> ConsolidateMerged(IEnumerable<string> originalBranches, string newBaseBranch);
    }
}