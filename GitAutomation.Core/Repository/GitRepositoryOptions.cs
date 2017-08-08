namespace GitAutomation.Repository
{
    public class GitRepositoryOptions
    {
        public string CheckoutPath { get; set; } = "/working";

        public string Repository { get; set; }
    }
}