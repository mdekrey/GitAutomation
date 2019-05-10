namespace GitAutomation.Web
{
    public class ConfigRepositoryOptions
    {
        public string Repository { get; set; }
        public string Password { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string CheckoutPath { get; set; }
        public string BranchName { get; set; }
    }
}