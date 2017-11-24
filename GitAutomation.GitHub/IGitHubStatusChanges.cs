using GitAutomation.GitService;

namespace GitAutomation.GitHub
{
    public interface IGitHubStatusChanges
    {
        void ReceiveCommitStatus(string commitSha, CommitStatus status);
    }
}