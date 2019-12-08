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
    public class CommitScriptShould
    {
        private readonly ConfigGitDirectory workingGitDirectory;

        public CommitScriptShould(ConfigGitDirectory workingGitDirectory)
        {
            this.workingGitDirectory = workingGitDirectory;
        }

        [Fact]
        public async Task CommitWithoutPushing()
        {
            using var directory = workingGitDirectory.CreateCopy();
            using var tempDir = directory.CreateCopy(new CloneOptions { BranchName = "git-config" });
            File.WriteAllText(Path.Combine(tempDir.Path, "test.txt"), "This is just a test commit, no need to update yaml.");

            var timestamp = DateTimeOffset.Now;
            await Invoke(tempDir.TemporaryDirectory, timestamp);

            using var newRepo = new LibGit2Sharp.Repository(tempDir.TemporaryDirectory.Path);
            using var originalRepo = new LibGit2Sharp.Repository(directory.Path);

            var original = originalRepo.Head.Tip.Sha;
            var next = newRepo.Head.Tip.Sha;
            Assert.NotEqual(original, next);
            Assert.Equal(original, newRepo.Head.Tip.Parents.Single().Sha);
        }

        private static ConfigRepositoryOptions StandardParameters(TemporaryDirectory checkout)
        {
            return new ConfigRepositoryOptions
            {
                Password = "",
                UserEmail = "author@example.com",
                UserName = "A U Thor",
                CheckoutPath = checkout.Path,
                BranchName = "git-config"
            };
        }

        private async Task<IList<StateUpdateEvent<IStandardAction>>> Invoke(TemporaryDirectory checkout, DateTimeOffset timestamp)
        {
            var resultList = new List<StateUpdateEvent<IStandardAction>>();
            var script = new CommitScript(
                Options.Create(StandardParameters(checkout)),
                new DispatchToList(resultList)
            );
            await script.Run(
                new CommitScript.CommitScriptParams(timestamp, "test commit"),
                LoggerFactory.Create(_ => { }).CreateLogger(this.GetType().FullName),
                SystemAgent.Instance
                );
            return resultList;
        }
    }
}
