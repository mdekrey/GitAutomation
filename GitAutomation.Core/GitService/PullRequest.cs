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
        public PullRequestState State { get; set; }
        public string SourceBranch { get; set; }
        public string TargetBranch { get; set; }
    }
}