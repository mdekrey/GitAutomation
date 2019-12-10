#nullable disable warnings

namespace GitAutomation.DomainModels.Git
{
    public class RepositoryConfiguration
    {
        public string Url { get; set; }

        // TODO - other credential types
        public string Password { get; set; }
    }
}
