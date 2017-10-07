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
        IObservable<ImmutableList<string>> DetectShallowUpstream(string branchName, bool asGroup);
        IObservable<ImmutableList<PullRequestWithReviews>> GetUpstreamPullRequests(string branchName);
        IObservable<string> LatestBranchName(BranchGroupDetails details);
        IObservable<string> GetNextCandidateBranch(BranchGroupDetails details, bool shouldMutate);
        IObservable<BranchGroupCompleteData> GetBranchDetails(string branchName);
        void ConsolidateBranches(IEnumerable<string> branchesToRemove, string targetBranch, IUnitOfWork unitOfWork);
        IObservable<ImmutableList<Repository.GitRef>> GetAllBranchRefs();
        IObservable<string> GetBranchRef(string branchName);
        IObservable<bool> HasOutstandingCommits(string upstreamBranch, string downstreamBranch);
        void NotifyPushedRemoteBranch(string downstreamBranch);
    }
}
