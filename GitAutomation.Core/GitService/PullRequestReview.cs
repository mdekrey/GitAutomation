namespace GitAutomation.GitService
{
    public class PullRequestReview
    {
        public string Username { get; set; }
        public ApprovalState State { get; set; }

        public enum ApprovalState
        {
            Approved,
            Pending,
            ChangesRequested,
        }
    }
}