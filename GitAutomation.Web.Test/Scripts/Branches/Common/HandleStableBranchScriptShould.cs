using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Actions;
using GitAutomation.State;
using GitAutomation.Web;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GitAutomation.Scripts.Branches.Common
{
    [Collection("GitBranch collection")]
    public class HandleStableBranchScriptShould
    {
        private const string UserEmail = "author@example.com";
        private const string UserName = "A U Thor";
        private readonly GitDirectory workingGitDirectory;

        public HandleStableBranchScriptShould(BranchGitDirectory workingGitDirectory)
        {
            this.workingGitDirectory = workingGitDirectory;
            using var newRepo = new Repository(workingGitDirectory.Path);
            newRepo.Refs.UpdateTarget("HEAD", "refs/heads/origin/master");
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
        public async Task UpdateStabilizedBranch()
        {
            using var tempDir = new TemporaryDirectory();

            // Act to receive the expected FSA's
            var result = await Invoke(workingGitDirectory.TemporaryDirectory, tempDir, "master", new Dictionary<string, BranchReserve>
            {
                { "master", new BranchReserve("dontcare", "dontcare", "Stable", 
                    ImmutableSortedDictionary<string, UpstreamReserve>.Empty,
                    includedBranches: new[] { ("origin/master", new BranchReserveBranch(BranchReserve.EmptyCommit, ImmutableSortedDictionary<string, string>.Empty)) }.ToImmutableSortedDictionary(b => b.Item1, b => b.Item2), 
                    outputCommit: BranchReserve.EmptyCommit, 
                    meta: ImmutableSortedDictionary<string, object>.Empty) }
            });

            // Assert that we're correct
            var standardAction = Assert.Single(result);

            using var repo = new Repository(workingGitDirectory.Path);
            var action = Assert.IsType<StabilizeNoUpstreamAction>(standardAction.Payload);
            Assert.Equal(repo.Branches["refs/heads/origin/master"].Tip.Sha, action.BranchCommits["origin/master"]);
        }

        // TODO - more tests


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

        private async Task<IList<StateUpdateEvent<IStandardAction>>> Invoke(TemporaryDirectory repository, TemporaryDirectory tempPath, string reserveName, Dictionary<string, BranchReserve> reserves)
        {
            using var repo = new Repository(repository.Path);
            var resultList = new List<StateUpdateEvent<IStandardAction>>();
            var script = new HandleStableBranchScript(
                new DispatchToList(resultList)
            );
            var reserve = reserves[reserveName];
            await script.Run(
                new ReserveScriptParameters("master", new ReserveFullState(
                    reserve,
                    reserve.IncludedBranches.Keys.ToDictionary(k => k, k => repo.Branches[k] != null ? repo.Branches[k].Tip.Sha : BranchReserve.EmptyCommit),
                    reserve.Upstream.Keys.ToDictionary(k => k, upstream => reserves.ContainsKey(upstream) ? reserves[upstream] : null)
                ), tempPath.Path),
                LoggerFactory.Create(_ => { }).CreateLogger(this.GetType().FullName),
                SystemAgent.Instance
                );
            return resultList;
        }
    }
}
