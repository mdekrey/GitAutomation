using GitAutomation.Orchestration.Actions;
using GitAutomation.Processes;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Orchestration
{
    class OrchestrationActions : IOrchestrationActions
    {
        private readonly IRepositoryOrchestration orchestration;

        public OrchestrationActions(IRepositoryOrchestration orchestration)
        {
            this.orchestration = orchestration;
        }

        public IObservable<OutputMessage> CheckDownstreamMerges(string downstreamBranch)
        {
            return orchestration.EnqueueAction(new MergeDownstreamAction(downstreamBranch: downstreamBranch));
        }

        public IObservable<OutputMessage> ReleaseToServiceLine(string releaseCandidateBranch, string serviceLineBranch, string tagName)
        {
            return orchestration.EnqueueAction(new ReleaseToServiceLineAction(releaseCandidateBranch, serviceLineBranch, tagName));
        }

    }
}
