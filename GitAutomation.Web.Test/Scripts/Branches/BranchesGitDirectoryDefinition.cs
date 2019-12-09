using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace GitAutomation.Scripts.Branches
{
    [CollectionDefinition("GitBranch collection")]
    public class BranchesGitDirectoryDefinition : ICollectionFixture<BranchGitDirectory>, ICollectionFixture<BranchGitDirectoryOrigin>
    { }

    public class BranchGitDirectoryOrigin : GitDirectory
    {
        public const string UserEmail = "author@example.com";
        public const string UserName = "A U Thor";

        public BranchGitDirectoryOrigin()
        {
            using var newRepo = new Repository(Path);
            newRepo.Refs.UpdateTarget("HEAD", "refs/heads/master");
            WriteUpdatesAndCommit(newRepo, "Initial Commit", new Dictionary<string, string> { { "readme.md", "This is a test" } });
            newRepo.Branches.Add("feature-a", newRepo.Head.Tip);
            Commands.Checkout(newRepo, "refs/heads/feature-a");
            WriteUpdatesAndCommit(newRepo, "Modify message", new Dictionary<string, string> { { "readme.md", "This is a simple test" } });
            newRepo.Branches.Add("infrastructure", newRepo.Branches["refs/heads/master"].Tip);
            Commands.Checkout(newRepo, "refs/heads/infrastructure");
            WriteUpdatesAndCommit(newRepo, "Modify message", new Dictionary<string, string> { { "readme.md", "This is a basic test" } });
            newRepo.Branches.Add("feature-b", newRepo.Branches["refs/heads/master"].Tip);
            Commands.Checkout(newRepo, "refs/heads/feature-b");
            WriteUpdatesAndCommit(newRepo, "Modify message", new Dictionary<string, string> { { "additional.md", "This is another file" } });

            Commands.Checkout(newRepo, "refs/heads/master");
        }

        private void WriteUpdatesAndCommit(Repository newRepo, string commitMessage, Dictionary<string, string> fileContents)
        {
            foreach (var file in fileContents)
            {
                File.WriteAllText(System.IO.Path.Combine(Path, file.Key), file.Value);
            }

            Commands.Stage(newRepo, "*");
            var author = new Signature(UserName, UserEmail, DateTimeOffset.Now);
            newRepo.Commit(commitMessage, author, author);
        }

    }

    public class BranchGitDirectory : GitDirectory
    {
        public const string UserEmail = "author@example.com";
        public const string UserName = "A U Thor";

        public BranchGitDirectory()
        {
            using var newRepo = new Repository(Path);
            newRepo.Refs.UpdateTarget("HEAD", "refs/heads/origin/master");
            WriteUpdatesAndCommit(newRepo, "Initial Commit", new Dictionary<string, string> { { "readme.md", "This is a test" } });
            newRepo.Branches.Add("origin/feature-a", newRepo.Head.Tip);
            Commands.Checkout(newRepo, "refs/heads/origin/feature-a");
            WriteUpdatesAndCommit(newRepo, "Modify message", new Dictionary<string, string> { { "readme.md", "This is a simple test" } });
            newRepo.Branches.Add("origin/infrastructure", newRepo.Branches["refs/heads/origin/master"].Tip);
            Commands.Checkout(newRepo, "refs/heads/origin/infrastructure");
            WriteUpdatesAndCommit(newRepo, "Modify message", new Dictionary<string, string> { { "readme.md", "This is a basic test" } });
            newRepo.Branches.Add("origin/feature-b", newRepo.Branches["refs/heads/origin/master"].Tip);
            Commands.Checkout(newRepo, "refs/heads/origin/feature-b");
            WriteUpdatesAndCommit(newRepo, "Modify message", new Dictionary<string, string> { { "additional.md", "This is another file" } });

            Commands.Checkout(newRepo, "refs/heads/origin/master");
        }

        private void WriteUpdatesAndCommit(Repository newRepo, string commitMessage, Dictionary<string, string> fileContents)
        {
            foreach (var file in fileContents)
            {
                File.WriteAllText(System.IO.Path.Combine(Path, file.Key), file.Value);
            }

            Commands.Stage(newRepo, "*");
            var author = new Signature(UserName, UserEmail, DateTimeOffset.Now);
            newRepo.Commit(commitMessage, author, author);
        }

    }
}
