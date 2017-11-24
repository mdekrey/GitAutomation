namespace GitAutomation.GitService
{
    public class PullRequestReview
    {
        public string Author { get; set; }
        public ApprovalState State { get; set; }
        public string Url { get; set; }
        public string SubmittedDate { get; set; }

        public enum ApprovalState
        {
            Approved,
            Pending,
            ChangesRequested,
        }
    }
}