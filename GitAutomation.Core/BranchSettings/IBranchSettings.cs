using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive;
using System.Text;
using GitAutomation.Work;
using System.Threading.Tasks;

namespace GitAutomation.BranchSettings
{
    public interface IBranchSettings
    {
        IObservable<ImmutableList<BranchBasicDetails>> GetConfiguredBranches();
        IObservable<BranchBasicDetails> GetBranchBasicDetails(string branchName);
        IObservable<BranchDetails> GetBranchDetails(string branchName);
        IObservable<ImmutableList<BranchBasicDetails>> GetDownstreamBranches(string branchName);
        IObservable<ImmutableList<BranchDepthDetails>> GetAllDownstreamBranches();
        IObservable<ImmutableList<BranchBasicDetails>> GetAllDownstreamBranches(string branchName);
        IObservable<ImmutableList<BranchBasicDetails>> GetUpstreamBranches(string branchName);
        IObservable<ImmutableList<BranchBasicDetails>> GetAllUpstreamBranches(string branchName);
        IObservable<ImmutableList<string>> GetAllUpstreamRemovableBranches(string branchName);
        Task<string> GetIntegrationBranch(string branchA, string branchB);

        void UpdateBranchSetting(string branchName, bool recreateFromUpstream, BranchType branchType, Work.IUnitOfWork work);
        void AddBranchPropagation(string upstreamBranch, string downstreamBranch, Work.IUnitOfWork work);
        void RemoveBranchPropagation(string upstreamBranch, string downstreamBranch, Work.IUnitOfWork work);
        void ConsolidateBranches(IEnumerable<string> branchesToRemove, string targetBranch, IUnitOfWork unitOfWork);
        void DeleteBranchSettings(string deletingBranch, IUnitOfWork unitOfWork);

        void CreateIntegrationBranch(string branchA, string branchB, string integrationBranchName, IUnitOfWork unitOfWork);
    }
}
