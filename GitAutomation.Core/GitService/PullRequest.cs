using System.Collections.Immutable;

namespace GitAutomation.GitService
{
    public class PullRequest
    {
        public PullRequest()
        {
        }

        public PullRequest(PullRequest original)
        {
            this.Id = original.Id;
            this.State = original.State;
            this.SourceBranch = original.SourceBranch;
            this.TargetBranch = original.TargetBranch;
        }

        public string Id { get; set; }
        public string Created { get; set; }
        public string Author { get; set; }
        public bool IsSystem { get; set; }
        public PullRequestState State { get; set; }
        public string SourceBranch { get; set; }
        public string TargetBranch { get; set; }
        public string Url { get; set; }

        /// <summary>
        /// Provides the reviews associated with the pull request, if they were fetched. Otherwise, null.
        /// </summary>
        public ImmutableList<PullRequestReview> Reviews { get; set; }
    }
}