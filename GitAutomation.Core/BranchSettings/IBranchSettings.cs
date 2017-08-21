using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive;
using System.Text;

namespace GitAutomation.BranchSettings
{
    public interface IBranchSettings
    {
        IObservable<ImmutableList<string>> GetConfiguredBranches();
        IObservable<BranchDetails> GetBranchDetails(string branchName);
        IObservable<ImmutableList<string>> GetDownstreamBranches(string branchName);
        IObservable<ImmutableList<string>> GetAllDownstreamBranches(string branchName);
        IObservable<ImmutableList<string>> GetUpstreamBranches(string branchName);
        IObservable<ImmutableList<string>> GetAllUpstreamBranches(string branchName);
        IObservable<ImmutableList<string>> GetAllUpstreamRemovableBranches(string branchName);

        void UpdateBranchSetting(string branchName, bool recreateFromUpstream, bool isServiceLine, Work.IUnitOfWork work);
        void AddBranchPropagation(string upstreamBranch, string downstreamBranch, Work.IUnitOfWork work);
        void RemoveBranchPropagation(string upstreamBranch, string downstreamBranch, Work.IUnitOfWork work);
        void ConsolidateServiceLine(string releaseCandidateBranch, string serviceLineBranch, Work.IUnitOfWork work);

    }
}
