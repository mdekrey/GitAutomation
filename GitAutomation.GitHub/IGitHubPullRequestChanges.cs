using GitAutomation.GitService;

namespace GitAutomation.GitHub
{
    internal interface IGitHubPullRequestChanges
    {
        void ReceivePullRequestUpdate(PullRequest pullRequest);
        void ReceivePullRequestReview(string id, PullRequestReview review, bool remove);
    }
}