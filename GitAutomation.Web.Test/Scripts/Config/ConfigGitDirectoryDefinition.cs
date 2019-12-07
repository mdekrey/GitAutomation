using Xunit;

namespace GitAutomation.Scripts.Config
{
    [CollectionDefinition("GitConfiguration collection")]
    public class ConfigGitDirectoryDefinition : ICollectionFixture<ConfigGitDirectory>
    { }
}
