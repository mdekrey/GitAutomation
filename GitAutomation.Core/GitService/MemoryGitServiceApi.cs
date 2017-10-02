using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.GitService
{
    class MemoryGitServiceApi : IGitServiceApi
    {
        readonly Dictionary<Tuple<string, string>, PullRequest> pullRequests = new Dictionary<Tuple<string, string>, PullRequest>();

        public Task<ImmutableList<CommitStatus>> GetCommitStatus(string commitSha)
        {
            return Task.FromResult(ImmutableList<CommitStatus>.Empty);
        }

        public Task<ImmutableList<PullRequestReview>> GetPullRequestReviews(string targetBranch)
        {
            return Task.FromResult(ImmutableList<PullRequestReview>.Empty);
        }

        public Task<ImmutableList<PullRequest>> GetPullRequests(PullRequestState? state = PullRequestState.Open, string targetBranch = null, string sourceBranch = null)
        {
            return Task.FromResult(
                pullRequests.Values
                    .Where(ex => (state == null || ex.State == state) && (targetBranch == null || ex.TargetBranch == targetBranch) && (sourceBranch == null || ex.SourceBranch == sourceBranch))
                    .ToImmutableList()
            );
        }

        public Task<bool> OpenPullRequest(string title, string targetBranch, string sourceBranch, string body = null)
        {
            if (!pullRequests.ContainsKey(new Tuple<string, string>(targetBranch, sourceBranch)))
            {
                pullRequests.Add(
                    new Tuple<string, string>(targetBranch, sourceBranch), 
                    new PullRequest { TargetBranch = targetBranch, SourceBranch = sourceBranch }
                );
            }
            return Task.FromResult(true);
        }
    }
}
