using GitAutomation.DomainModels.Git;
using System.Collections.Generic;
#nullable disable warnings

namespace GitAutomation.Web
{
    public class TargetRepositoryOptions
    {
        public Dictionary<string, RepositoryConfiguration> Remotes { get; set; } = new Dictionary<string, RepositoryConfiguration>();
        public GitIdentity GitIdentity { get; set; }
        public string CheckoutPath { get; set; }
    }
}