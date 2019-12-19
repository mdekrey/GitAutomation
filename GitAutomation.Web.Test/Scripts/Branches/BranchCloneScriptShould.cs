using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Actions;
using GitAutomation.DomainModels.Git;
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
        private readonly GitDirectory workingGitDirectory;

        public BranchCloneScriptShould(BranchGitDirectoryOrigin workingGitDirectory)
        {
            this.workingGitDirectory = workingGitDirectory;
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
            var canonicalNames = newRepo.Branches.Select(b => b.CanonicalName).OrderBy(n => n).ToArray();
            Assert.Equal(canonicalNames,
                new[] { "refs/heads/origin/master", "refs/heads/origin/feature-a", "refs/heads/origin/feature-b", "refs/heads/origin/infrastructure" }.OrderBy(n => n));
        }

        // TODO - more tests


        private static TargetRepositoryOptions StandardParameters(TemporaryDirectory repository, TemporaryDirectory checkout)
        {
            return new TargetRepositoryOptions
            {
                Remotes = new Dictionary<string, RepositoryConfiguration>
                {
                    { "origin", new RepositoryConfiguration { Url = repository.Path } }
                },
                GitIdentity = new GitIdentity
                {
                    UserEmail = TestingConstants.UserEmail,
                    UserName = TestingConstants.UserName,
                },
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
