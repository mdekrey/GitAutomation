using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.GitService
{
    public interface IGitServiceApi
    {
        Task<bool> OpenPullRequest(string title, string targetBranch, string sourceBranch, string body = null);
        Task<ImmutableList<PullRequest>> GetPullRequests(PullRequestState? state = PullRequestState.Open, string targetBranch = null, string sourceBranch = null);
        Task<ImmutableList<PullRequestReview>> GetPullRequestReviews(string id);

        Task<ImmutableList<CommitStatus>> GetCommitStatus(string commitSha);
    }
}
