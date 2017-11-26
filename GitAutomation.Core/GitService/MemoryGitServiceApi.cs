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

        public Task<string> GetBranchUrl(string name)
        {
            return Task.FromResult<string>(null);
        }

        public Task<ImmutableDictionary<string, ImmutableList<CommitStatus>>> GetCommitStatuses(ImmutableList<string> commitSha)
        {
            return Task.FromResult(commitSha.Distinct().ToImmutableDictionary(k => k, _ => ImmutableList<CommitStatus>.Empty));
        }
        
        public Task<ImmutableList<PullRequest>> GetPullRequests(PullRequestState? state = PullRequestState.Open, string targetBranch = null, string sourceBranch = null, bool includeReviews = false, PullRequestAuthorMode authorMode = PullRequestAuthorMode.All)
        {
            return Task.FromResult(
                pullRequests.Values
                    .Where(ex => (state == null || ex.State == state) && (targetBranch == null || ex.TargetBranch == targetBranch) && (sourceBranch == null || ex.SourceBranch == sourceBranch))
                    .Where(ex => authorMode != PullRequestAuthorMode.NonSystemOnly)
                    .ToImmutableList()
            );
        }

        public Task MigrateOrClosePullRequests(string fromBranch, string toBranch)
        {
            return Task.CompletedTask;
        }

        public Task<bool> OpenPullRequest(string title, string targetBranch, string sourceBranch, string body = null)
        {
            if (!pullRequests.ContainsKey(new Tuple<string, string>(targetBranch, sourceBranch)))
            {
                pullRequests.Add(
                    new Tuple<string, string>(targetBranch, sourceBranch), 
                    new PullRequest { TargetBranch = targetBranch, SourceBranch = sourceBranch, Reviews = ImmutableList<PullRequestReview>.Empty }
                );
            }
            return Task.FromResult(true);
        }
    }
}
