namespace GitAutomation.GitService
{
    public class PullRequest
    {
        public string Id { get; set; }
        public PullRequestState State { get; set; }
        public string SourceBranch { get; set; }
        public string TargetBranch { get; set; }
    }
}