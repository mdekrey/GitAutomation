using GitAutomation.Serialization.Defaults;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace GitAutomation.Scripts.Config
{
    public class ConfigGitDirectory : GitDirectory
    {
        public ConfigGitDirectory()
        {
            DefaultsWriter.WriteDefaultsToDirectory(TemporaryDirectory.Path).Wait();

            using var repo = new LibGit2Sharp.Repository(TemporaryDirectory.Path);
            repo.Refs.UpdateTarget("HEAD", "refs/heads/git-config");
            Commands.Stage(repo, "*");
            repo.Commit(
                "Initial commit", 
                new Signature("A U Thor", "author@example.com", DateTimeOffset.Now), 
                new Signature("A U Thor", "author@example.com", DateTimeOffset.Now)
            );
        }
    }


    [CollectionDefinition("GitConfiguration collection")]
    public class ConfigGitDirectoryDefinition : ICollectionFixture<ConfigGitDirectory>
    { }
}
