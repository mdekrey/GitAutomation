using GitAutomation.GitService;

namespace GitAutomation.GitHub
{
    internal interface IGitHubPullRequestChanges
    {
        void ReceivePullRequestUpdate(PullRequest pullRequest);

    }
}