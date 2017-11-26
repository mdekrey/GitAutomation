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
        Task<ImmutableList<PullRequest>> GetPullRequests(PullRequestState? state = PullRequestState.Open, string targetBranch = null, string sourceBranch = null, bool includeReviews = false, PullRequestAuthorMode authorMode = PullRequestAuthorMode.All);
        Task MigrateOrClosePullRequests(string fromBranch, string toBranch);

        Task<ImmutableDictionary<string, ImmutableList<CommitStatus>>> GetCommitStatuses(ImmutableList<string> commitSha);
        Task<string> GetBranchUrl(string name);
    }
}
