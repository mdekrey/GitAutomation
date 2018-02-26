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
        IObservable<ImmutableList<BranchGroup>> GetConfiguredBranches();
        IObservable<BranchGroup> GetBranchBasicDetails(string branchName);
        IObservable<BranchGroupCompleteData> GetBranchDetails(string branchName);
        IObservable<ImmutableList<BranchGroup>> GetDownstreamBranches(string branchName);
        IObservable<ImmutableList<BranchGroupCompleteData>> GetAllDownstreamBranches();
        IObservable<ImmutableList<BranchGroup>> GetAllDownstreamBranches(string branchName);
        IObservable<ImmutableList<BranchGroup>> GetUpstreamBranches(string branchName);
        IObservable<ImmutableList<BranchGroup>> GetAllUpstreamBranches(string branchName);
        IObservable<ImmutableList<string>> GetAllUpstreamRemovableBranches(string branchName);
        Task<string> GetIntegrationBranch(string branchA, string branchB);
        Task<ImmutableList<string>> GetIntegrationBranches(ImmutableList<string> upstreamBranchGroups);

        void UpdateBranchSetting(string branchName, UpstreamMergePolicy upstreamMergePolicy, BranchGroupType branchType, Work.IUnitOfWork work);
        void AddBranchPropagation(string upstreamBranch, string downstreamBranch, Work.IUnitOfWork work);
        void RemoveBranchPropagation(string upstreamBranch, string downstreamBranch, Work.IUnitOfWork work);
        void ConsolidateBranches(IEnumerable<string> branchesToRemove, string targetBranch, IUnitOfWork unitOfWork);
        void DeleteBranchSettings(string deletingBranch, IUnitOfWork unitOfWork);

        void CreateIntegrationBranch(string branchA, string branchB, string integrationBranchName, IUnitOfWork unitOfWork);
    }
}
