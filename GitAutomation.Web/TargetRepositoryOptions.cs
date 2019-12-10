using System.Collections.Generic;
#nullable disable warnings

namespace GitAutomation.Web
{
    public class TargetRepositoryOptions
    {
        public Dictionary<string, RemoteRepositoryOptions> Remotes { get; set; } = new Dictionary<string, RemoteRepositoryOptions>();
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string CheckoutPath { get; set; }
    }
}