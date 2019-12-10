using GitAutomation.DomainModels;
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

namespace GitAutomation.Scripts.Config
{
    [Collection("GitConfiguration collection")]
    public class PushScriptShould
    {
        private readonly GitDirectory workingGitDirectory;

        public PushScriptShould(ConfigGitDirectory workingGitDirectory)
        {
            this.workingGitDirectory = workingGitDirectory;
        }

        [Fact]
        public async Task PushCommittedChanges()
        {
            using var directory = workingGitDirectory.CreateCopy(new CloneOptions { IsBare = true });
            using var tempDir = directory.CreateClone(new CloneOptions { BranchName = "git-config" });

            File.WriteAllText(Path.Combine(tempDir.Path, "test.txt"), "This is just a test commit, no need to update yaml.");

            var configRepositoryOptions = StandardParameters(tempDir.TemporaryDirectory);
            using var newRepo = new LibGit2Sharp.Repository(tempDir.TemporaryDirectory.Path);
            Commands.Stage(newRepo, "*");
            var author = new Signature(configRepositoryOptions.UserName, configRepositoryOptions.UserEmail, DateTimeOffset.Now);
            newRepo.Commit("Test commit", author, author);

            var timestamp = DateTimeOffset.Now;
            var actions = await Invoke(tempDir.TemporaryDirectory, timestamp);

            using var originalRepo = new LibGit2Sharp.Repository(directory.Path);

            var original = originalRepo.Head.Tip.Sha;
            var next = newRepo.Head.Tip.Sha;
            Assert.Equal(original, next);
        }

        private static ConfigRepositoryOptions StandardParameters(TemporaryDirectory checkout)
        {
            return new ConfigRepositoryOptions
            {
                UserEmail = "author@example.com",
                UserName = "A U Thor",
                CheckoutPath = checkout.Path,
                BranchName = "git-config"
            };
        }

        private async Task<IList<StateUpdateEvent<IStandardAction>>> Invoke(TemporaryDirectory checkout, DateTimeOffset timestamp)
        {
            var resultList = new List<StateUpdateEvent<IStandardAction>>();
            var script = new PushScript(
                Options.Create(StandardParameters(checkout)),
                new DispatchToList(resultList)
            );
            using var loggerFactory = LoggerFactory.Create(_ => { });
            await script.Run(
                new PushScript.PushScriptParams(timestamp),
                loggerFactory.CreateLogger(this.GetType().FullName),
                SystemAgent.Instance
                );
            return resultList;
        }
    }
}
