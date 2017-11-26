using GitAutomation.Orchestration.Actions;
using GitAutomation.Processes;
using GitAutomation.Repository;
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

        #region Reset

        public IObservable<IRepositoryActionEntry> DeleteRepository()
        {
            return orchestration.EnqueueAction(new ClearAction());
        }

        #endregion


        public IObservable<IRepositoryActionEntry> CheckForUpdates()
        {
            return orchestration.EnqueueAction(new UpdateAction());
        }

        public void CheckForUpdatesOnBranch(string branchName)
        {
            orchestration.EnqueueAction(new UpdateAction(branchName));
        }

        public IObservable<IRepositoryActionEntry> DeleteBranch(string branchName, DeleteBranchMode mode)
        {
            return orchestration.EnqueueAction(new DeleteBranchAction(branchName, mode));
        }

        public IObservable<IRepositoryActionEntry> CheckDownstreamMerges(string downstreamBranch)
        {
            return orchestration.EnqueueAction(new MergeDownstreamAction(downstreamBranch: downstreamBranch));
        }

        public IObservable<IRepositoryActionEntry> ReleaseToServiceLine(string releaseCandidateBranch, string serviceLineBranch, string tagName, bool autoConsolidate)
        {
            return orchestration.EnqueueAction(new ReleaseToServiceLineAction(releaseCandidateBranch, serviceLineBranch, tagName, autoConsolidate));
        }

        public IObservable<IRepositoryActionEntry> ConsolidateMerged(IEnumerable<string> originalBranches, string newBaseBranch)
        {
            return orchestration.EnqueueAction(new ConsolidateMergedAction(originalBranches, newBaseBranch));
        }
    }
}
