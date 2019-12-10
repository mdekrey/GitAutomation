#nullable disable warnings

using GitAutomation.DomainModels.Git;

namespace GitAutomation.Web
{
    public class ConfigRepositoryOptions
    {
        public RepositoryConfiguration Repository { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string CheckoutPath { get; set; }
        public string BranchName { get; set; }
    }
}