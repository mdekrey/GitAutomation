#nullable disable warnings

using GitAutomation.DomainModels.Git;

namespace GitAutomation.Web
{
    public class ConfigRepositoryOptions
    {
        public RepositoryConfiguration Repository { get; set; }
        public GitIdentity GitIdentity { get; set; }
        public string CheckoutPath { get; set; }
        public string BranchName { get; set; }
    }
}