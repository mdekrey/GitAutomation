namespace GitAutomation.GitService
{
    public class CommitStatus
    {
        public string Key { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public StatusState State { get; set; }

        public enum StatusState
        {
            Success,
            Error,
            Pending,
        }
    }
}