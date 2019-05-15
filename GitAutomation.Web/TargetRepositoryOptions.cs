using System.Collections.Generic;

namespace GitAutomation.Web
{
    public class TargetRepositoryOptions
    {
        public Dictionary<string, RemoteRepositoryOptions> Remotes { get; set; }
        public string Repository { get; set; }
        public string Password { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string CheckoutPath { get; set; }
    }
}