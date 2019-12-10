using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Newtonsoft.Json;
using System.Linq;
using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Configuration.Actions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using GitAutomation.State;
using Microsoft.Extensions.Logging;
using GitAutomation.Web;
using LibGit2Sharp;

namespace GitAutomation.Scripts.Config
{
    [Collection("GitConfiguration collection")]
    public class CloneScriptShould
    {
        private readonly ConfigGitDirectory workingGitDirectory;

        public CloneScriptShould(ConfigGitDirectory workingGitDirectory)
        {
            this.workingGitDirectory = workingGitDirectory;
        }

        [Fact]
        public async Task RecognizeAnEmptyBranch()
        {
            using var directory = new GitDirectory();
            using var tempDir = new TemporaryDirectory();
            // Arrange it to already have a clone
            LibGit2Sharp.Repository.Clone(directory.Path, tempDir.Path);

            // Act to receive the expected FSA's
            var timestamp = DateTimeOffset.Now;
            var result = await Invoke(directory.TemporaryDirectory, tempDir, timestamp);

            // Assert that we're correct
            var standardAction = Assert.Single(result);
            var action = Assert.IsType<GitNoBranchAction>(standardAction.Payload);
            Assert.Equal(timestamp, action.StartTimestamp);
        }

        [Fact]
        public async Task HandleAlreadyClonedDirectories()
        {
            using var directory = workingGitDirectory.CreateCopy();
            // Arrange it to already have a clone
            using var tempDir = directory.CreateCopy(new LibGit2Sharp.CloneOptions { BranchName = "git-config" });

            // Act to receive the expected FSA's
            var timestamp = DateTimeOffset.Now;
            var result = await Invoke(directory.TemporaryDirectory, tempDir.TemporaryDirectory, timestamp);

            // Assert that we're correct
            var standardAction = Assert.Single(result);
            var action = Assert.IsType<ReadyToLoadAction>(standardAction.Payload);
            Assert.Equal(timestamp, action.StartTimestamp);
        }

        [Fact]
        public async Task CloneFreshDirectories()
        {
            using var directory = workingGitDirectory.CreateCopy();
            using var tempDir = new TemporaryDirectory();

            // Act to receive the expected FSA's
            var timestamp = DateTimeOffset.Now;
            var result = await Invoke(directory.TemporaryDirectory, tempDir, timestamp);

            // Assert that we're correct
            var standardAction = Assert.Single(result);
            var action = Assert.IsType<ReadyToLoadAction>(standardAction.Payload);
            Assert.Equal(timestamp, action.StartTimestamp);
            Assert.True(Repository.IsValid(tempDir.Path));
            using var repo = new Repository(tempDir.Path);
            Assert.False(repo.Info.IsBare);
        }

        [Fact]
        public async Task HandleBadTargetDirectories()
        {
            using var directory = new TemporaryDirectory();
            using var tempDir = new TemporaryDirectory();

            // Act to receive the expected FSA's
            var timestamp = DateTimeOffset.Now;
            var result = await Invoke(directory, tempDir, timestamp);

            // Assert that we're correct
            var standardAction = Assert.Single(result);
            var action = Assert.IsType<GitPasswordIncorrectAction>(standardAction.Payload);
            Assert.Equal(timestamp, action.StartTimestamp);
        }

        private static ConfigRepositoryOptions StandardParameters(TemporaryDirectory repository, TemporaryDirectory checkout)
        {
            return new ConfigRepositoryOptions
            {
                Repository = repository.Path,
                Password = "",
                UserEmail = "author@example.com",
                UserName = "A U Thor",
                CheckoutPath = checkout.Path,
                BranchName = "git-config"
            };
        }

        private async Task<IList<StateUpdateEvent<IStandardAction>>> Invoke(TemporaryDirectory repository, TemporaryDirectory checkout, DateTimeOffset timestamp)
        {
            var resultList = new List<StateUpdateEvent<IStandardAction>>();
            var script = new CloneScript(
                Options.Create(StandardParameters(repository, checkout)),
                new DispatchToList(resultList)
            );
            using var loggerFactory = LoggerFactory.Create(_ => { });
            await script.Run(
                new CloneScript.CloneScriptParams(timestamp),
                loggerFactory.CreateLogger(this.GetType().FullName),
                SystemAgent.Instance
                );
            return resultList;
        }
    }
}
