using System;
using GitAutomation.Processes;

namespace GitAutomation.Orchestration
{
    public interface IOrchestrationActions
    {
        IObservable<OutputMessage> CheckDownstreamMerges(string downstreamBranch);
        IObservable<OutputMessage> ConsolidateServiceLine(string releaseCandidateBranch, string serviceLineBranch, string tagName);
    }
}