using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Actions;
using GitAutomation.State;
using GitAutomation.Web;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GitAutomation.Scripts.Branches
{
    [Collection("GitBranch collection")]
    public class BranchCloneScriptShould
    {
        private const string UserEmail = "author@example.com";
        private const string UserName = "A U Thor";
        private readonly GitDirectory workingGitDirectory;

        public BranchCloneScriptShould(BranchGitDirectory workingGitDirectory)
        {
            this.workingGitDirectory = workingGitDirectory;
            WriteUpdatesAndCommit(workingGitDirectory.Path, "InitialCommit", new Dictionary<string, string> { { "readme.md", "This is a test" } });
        }

        private void WriteUpdatesAndCommit(string path, string commitMessage, Dictionary<string, string> fileContents)
        {
            foreach (var file in fileContents)
            {
                File.WriteAllText(Path.Combine(path, file.Key), file.Value);
            }

            using var newRepo = new Repository(path);
            Commands.Stage(newRepo, "*");
            var author = new Signature(UserName, UserEmail, DateTimeOffset.Now);
            newRepo.Commit(commitMessage, author, author);
        }

        [Fact]
        public async Task CloneFreshRemotes()
        {
            using var tempDir = new TemporaryDirectory();

            // Act to receive the expected FSA's
            var timestamp = DateTimeOffset.Now;
            var result = await Invoke(workingGitDirectory.TemporaryDirectory, tempDir, timestamp);

            // Assert that we're correct
            var standardAction = Assert.Single(result);
            
            var action = Assert.IsType<FetchedAction>(standardAction.Payload);
            Assert.Equal(timestamp, action.StartTimestamp);

            using var newRepo = new Repository(tempDir.Path);
            var cannonicalNames = newRepo.Branches.Select(b => b.CanonicalName).ToArray();
            var name = Assert.Single(cannonicalNames);
            Assert.Equal("refs/heads/origin/master", name);
        }


        private static TargetRepositoryOptions StandardParameters(TemporaryDirectory repository, TemporaryDirectory checkout)
        {
            return new TargetRepositoryOptions
            {
                Remotes = new Dictionary<string, RemoteRepositoryOptions>
                {
                    { "origin", new RemoteRepositoryOptions { Repository = repository.Path } }
                },
                Repository = repository.Path,
                Password = "",
                UserEmail = UserEmail,
                UserName = UserName,
                CheckoutPath = checkout.Path,
            };
        }

        private async Task<IList<StateUpdateEvent<IStandardAction>>> Invoke(TemporaryDirectory repository, TemporaryDirectory checkout, DateTimeOffset timestamp)
        {
            var resultList = new List<StateUpdateEvent<IStandardAction>>();
            var script = new CloneScript(
                Options.Create(StandardParameters(repository, checkout)),
                new DispatchToList(resultList)
            );
            await script.Run(
                new CloneScript.CloneScriptParams(timestamp),
                LoggerFactory.Create(_ => { }).CreateLogger(this.GetType().FullName),
                SystemAgent.Instance
                );
            return resultList;
        }
    }
}
