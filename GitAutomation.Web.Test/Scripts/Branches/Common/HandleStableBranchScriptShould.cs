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
        private readonly GitDirectory workingGitDirectory;

        public HandleStableBranchScriptShould(BranchGitDirectory workingGitDirectory)
        {
            this.workingGitDirectory = workingGitDirectory;
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
                    meta: ImmutableSortedDictionary<string, string>.Empty) }
            });

            // Assert that we're correct
            var standardAction = Assert.Single(result);

            using var repo = new Repository(workingGitDirectory.Path);
            var action = Assert.IsType<StabilizeNoUpstreamAction>(standardAction.Payload);
            Assert.Equal(repo.Branches["refs/heads/origin/master"].Tip.Sha, action.BranchCommits["origin/master"]);
        }

        // TODO - more tests

        private async Task<IList<StateUpdateEvent<IStandardAction>>> Invoke(TemporaryDirectory repository, TemporaryDirectory tempPath, string reserveName, Dictionary<string, BranchReserve> reserves)
        {
            using var repo = new Repository(repository.Path);
            var resultList = new List<StateUpdateEvent<IStandardAction>>();
            var script = new HandleStableBranchScript(
                new DispatchToList(resultList)
            );
            var reserve = reserves[reserveName];
            using var loggerFactory = LoggerFactory.Create(_ => { });
            await script.Run(
                new ReserveScriptParameters("master", new ReserveFullState(
                    reserve,
                    reserve.IncludedBranches.Keys.ToDictionary(k => k, k => repo.Branches[k] != null ? repo.Branches[k].Tip.Sha : BranchReserve.EmptyCommit),
                    reserve.Upstream.Keys.ToDictionary(k => k, upstream => reserves.ContainsKey(upstream) ? reserves[upstream] : throw new InvalidOperationException($"Unknown Reserve: {upstream}"))
                ), tempPath.Path),
                loggerFactory.CreateLogger(this.GetType().FullName),
                SystemAgent.Instance
                );
            return resultList;
        }
    }
}
