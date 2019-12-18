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
            CreateRepository(Path);
        }

        public static Repository CreateRepository(string path, string branchNamePrefix = "")
        {
            var newRepo = new Repository(path);
            newRepo.Refs.UpdateTarget("HEAD", $"refs/heads/{branchNamePrefix}master");
            WriteUpdatesAndCommit(newRepo, "Initial Commit", new Dictionary<string, string> { { "readme.md", "This is a test" } });
            newRepo.Branches.Add($"{branchNamePrefix}feature-a", newRepo.Head.Tip);
            Commands.Checkout(newRepo, $"refs/heads/{branchNamePrefix}feature-a");
            WriteUpdatesAndCommit(newRepo, "Modify message", new Dictionary<string, string> { { "readme.md", "This is a simple test" } });
            newRepo.Branches.Add($"{branchNamePrefix}infrastructure", newRepo.Branches[$"refs/heads/{branchNamePrefix}master"].Tip);
            Commands.Checkout(newRepo, $"refs/heads/{branchNamePrefix}infrastructure");
            WriteUpdatesAndCommit(newRepo, "Modify message", new Dictionary<string, string> { { "readme.md", "This is a basic test" } });
            newRepo.Branches.Add($"{branchNamePrefix}feature-b", newRepo.Branches[$"refs/heads/{branchNamePrefix}master"].Tip);
            Commands.Checkout(newRepo, $"refs/heads/{branchNamePrefix}feature-b");
            WriteUpdatesAndCommit(newRepo, "Modify message", new Dictionary<string, string> { { "additional.md", "This is another file" } });

            Commands.Checkout(newRepo, $"refs/heads/{branchNamePrefix}master");
            return newRepo;
        }

        private static void WriteUpdatesAndCommit(Repository newRepo, string commitMessage, Dictionary<string, string> fileContents)
        {
            foreach (var file in fileContents)
            {
                File.WriteAllText(System.IO.Path.Combine(newRepo.Info.WorkingDirectory, file.Key), file.Value);
            }

            Commands.Stage(newRepo, "*");
            var author = new Signature(UserName, UserEmail, DateTimeOffset.Now);
            newRepo.Commit(commitMessage, author, author);
        }

    }

    public class BranchGitDirectory : GitDirectory
    {
        public BranchGitDirectory()
        {
            BranchGitDirectoryOrigin.CreateRepository(Path, "origin/");
        }

        private void WriteUpdatesAndCommit(Repository newRepo, string commitMessage, Dictionary<string, string> fileContents)
        {
            foreach (var file in fileContents)
            {
                File.WriteAllText(System.IO.Path.Combine(Path, file.Key), file.Value);
            }

            Commands.Stage(newRepo, "*");
            var author = new Signature(BranchGitDirectoryOrigin.UserName, BranchGitDirectoryOrigin.UserEmail, DateTimeOffset.Now);
            newRepo.Commit(commitMessage, author, author);
        }

    }
}
