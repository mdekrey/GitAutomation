using System.Collections.Immutable;

namespace GitAutomation.GitService
{
    public class PullRequestWithReviews : PullRequest
    {
        public PullRequestWithReviews()
        {
        }

        public PullRequestWithReviews(PullRequest original) : base(original)
        {

        }

        public ImmutableList<PullRequestReview> Reviews { get; set; }
    }
}