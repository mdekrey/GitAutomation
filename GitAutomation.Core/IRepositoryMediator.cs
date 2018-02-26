using GitAutomation.BranchSettings;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using GitAutomation.Work;
using GitAutomation.GitService;

namespace GitAutomation
{
    public interface IRepositoryMediator
    {
        IObservable<ImmutableList<BranchGroupCompleteData>> AllBranches();
        IObservable<ImmutableList<BranchGroupCompleteData>> AllBranchesHierarchy();
        Task<ImmutableList<string>> DetectUpstream(string actualBranchName);
        Task<ImmutableList<string>> DetectShallowUpstream(string branchName, bool asGroup);
        IObservable<ImmutableList<string>> DetectShallowUpstreamServiceLines(string branchName);
        IObservable<ImmutableList<PullRequest>> GetUpstreamPullRequests(string branchName);
        IObservable<string> LatestBranchName(BranchGroup details);
        IObservable<string> GetNextCandidateBranch(BranchGroup details);
        IObservable<BranchGroupCompleteData> GetBranchDetails(string branchName);
        void ConsolidateBranches(IEnumerable<string> branchesToRemove, string targetBranch, IUnitOfWork unitOfWork);
        IObservable<ImmutableList<Repository.GitRef>> GetAllBranchRefs();
        IObservable<string> GetBranchRef(string branchName);
        Task<bool> HasOutstandingCommits(string upstreamBranch, string downstreamBranch);
        Task BranchUpdated(string downstreamBranch, string newValue, string oldValue);
        IObservable<ImmutableList<BranchGroup>> GetConfiguredBranchGroups();
        IObservable<ImmutableList<string>> RecommendNewGroups();
        void CheckForUpdates();
        void CheckForUpdatesOnBranch(string branchName);
        /// <summary>
        /// Flag a given gitref as being "bad" until it is updated.
        /// </summary>
        void FlagBadGitRef(string branch, string commit, string reasonCode, DateTimeOffset? timestamp = null);
        Task<Repository.BadBranchInfo> GetBadBranchInfo(string branchName);
        Task ResetBadBranchStatus(string branchName);
        Task<bool?> CanMerge(string branchNameA, string branchNameB);
        Task MarkCanMerge(string branchNameA, string branchNameB, bool canMerge);

        /// <returns>True if new branches were added, otherwise false.</returns>
        Task<bool> AddAdditionalIntegrationBranches(BranchGroupCompleteData details, IUnitOfWork unitOfWork);
    }
}
