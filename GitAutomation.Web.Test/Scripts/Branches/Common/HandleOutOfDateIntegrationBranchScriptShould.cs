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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GitAutomation.Scripts.Branches.Common
{
    [Collection("GitBranch collection")]
    public class HandleOutOfDateIntegrationBranchScriptShould
    {
        private readonly GitDirectory workingGitDirectory;

        public HandleOutOfDateIntegrationBranchScriptShould(BranchGitDirectory workingGitDirectory)
        {
            this.workingGitDirectory = workingGitDirectory;
            using var newRepo = new Repository(workingGitDirectory.Path);
        }

        [Fact]
        public async Task MergeUpstreamBranch()
        {
            using var gitDir = new GitDirectory(isBare: true);
            using var checkout = workingGitDirectory.CreateCopy(new CloneOptions { IsBare = true });
            using var tempDir = new TemporaryDirectory();

            using var repo = new Repository(checkout.Path);
            var originalFeatureB = repo.Branches["origin/feature-b"].Tip.Sha;

            // Act to receive the expected FSA's
            var result = await Invoke(gitDir.TemporaryDirectory, tempDir, checkout, "feature-b", new Dictionary<string, BranchReserve>
            {
                { "feature-b", new BranchReserve("dontcare", "Automatic", "Stable",
                    Upstream(("infrastructure", new UpstreamReserve(repo.Branches["origin/infrastructure"].Tip.Sha))),
                    IncludedBranches(
                        ("origin/feature-b", new BranchReserveBranch(repo.Branches["origin/feature-b"].Tip.Sha, Metadata(("Role", "Output"))))
                    ),
                    outputCommit: repo.Branches["origin/feature-b"].Tip.Sha,
                    meta: Metadata()) },
                { "infrastructure", new BranchReserve("dontcare", "dontcare", "Stable",
                    Upstream(),
                    IncludedBranches(
                        ("origin/infrastructure", new BranchReserveBranch(repo.Branches["origin/infrastructure"].Tip.Sha, Metadata(("Role", "Output"))))
                    ),
                    outputCommit: repo.Branches["origin/infrastructure"].Tip.Sha,
                    meta: Metadata()) }
            });

            // Assert that we're correct
            var standardAction = Assert.Single(result);

            using var targetRepo = new Repository(gitDir.Path);

            var action = Assert.IsType<StabilizePushedReserveAction>(standardAction.Payload);
            Assert.NotEqual(originalFeatureB, action.BranchCommits["origin/feature-b"]);
            Assert.Equal(targetRepo.Branches["refs/heads/feature-b"].Tip.Sha, action.BranchCommits["origin/feature-b"]);
            Assert.Empty(action.ReserveOutputCommits);
            Assert.Null(action.NewOutput);
            Assert.Equal("feature-b", action.Reserve);
        }

        [Fact]
        public async Task HandleUpstreamConflictingBranch()
        {
            using var gitDir = new GitDirectory(isBare: true);
            using var checkout = workingGitDirectory.CreateCopy(new CloneOptions { IsBare = true });
            using var tempDir = new TemporaryDirectory();

            using var repo = new Repository(checkout.Path);
            var originalFeatureA = repo.Branches["origin/feature-a"].Tip.Sha;
            var originalInfrastructure = repo.Branches["origin/infrastructure"].Tip.Sha;

            // Act to receive the expected FSA's
            var result = await Invoke(gitDir.TemporaryDirectory, tempDir, checkout, "feature-a", new Dictionary<string, BranchReserve>
            {
                { "feature-a", new BranchReserve("dontcare", "Automatic", "Stable",
                    Upstream(("infrastructure", new UpstreamReserve(repo.Branches["origin/infrastructure"].Tip.Sha))),
                    IncludedBranches(
                        ("origin/feature-a", new BranchReserveBranch(repo.Branches["origin/feature-a"].Tip.Sha, Metadata(("Role", "Output"))))
                    ),
                    outputCommit: repo.Branches["origin/feature-a"].Tip.Sha,
                    meta: Metadata()) },
                { "infrastructure", new BranchReserve("dontcare", "dontcare", "Stable",
                    Upstream(),
                    IncludedBranches(
                        ("origin/infrastructure", new BranchReserveBranch(repo.Branches["origin/infrastructure"].Tip.Sha, Metadata(("Role", "Output"))))
                    ),
                    outputCommit: repo.Branches["origin/infrastructure"].Tip.Sha,
                    meta: Metadata()) }
            });

            // Assert that we're correct
            using var targetRepo = new Repository(gitDir.Path);
            Assert.Collection(result,
            standardAction =>
            {
                var action = Assert.IsType<RequestManualPullAction>(standardAction.Payload);
                Assert.Equal("origin/merge/feature-a_infrastructure", action.SourceBranch);
                Assert.Equal("origin/feature-a", action.TargetBranch);
            },
            standardAction =>
            {
                var action = Assert.IsType<ManualInterventionNeededAction>(standardAction.Payload);
                Assert.Empty(action.BranchCommits);
                Assert.Empty(action.ReserveOutputCommits);
                Assert.Equal("Conflicted", action.State);
                Assert.Equal("feature-a", action.Reserve);
                var newBranch = Assert.Single(action.NewBranches);
                Assert.Equal("origin/merge/feature-a_infrastructure", newBranch.Name);
                Assert.Equal(originalInfrastructure, targetRepo.Branches["merge/feature-a_infrastructure"].Tip.Sha);
                Assert.Equal(originalInfrastructure, newBranch.Commit);
                Assert.Equal("Integration", newBranch.Role);
                Assert.Equal("infrastructure", newBranch.Source);
            });
        }

        [Fact]
        public async Task HandlePeerConflictingBranch()
        {
            using var gitDir = new GitDirectory(isBare: true);
            using var checkout = workingGitDirectory.CreateCopy(new CloneOptions { IsBare = true });
            using var tempDir = new TemporaryDirectory();

            using var repo = new Repository(checkout.Path);
            var originalFeatureA = repo.Branches["origin/feature-a"].Tip.Sha;
            var originalInfrastructure = repo.Branches["origin/infrastructure"].Tip.Sha;

            // Act to receive the expected FSA's
            var result = await Invoke(gitDir.TemporaryDirectory, tempDir, checkout, "feature-b", new Dictionary<string, BranchReserve>
            {
                { "feature-b", new BranchReserve("dontcare", "Automatic", "Stable",
                    Upstream(("infrastructure", new UpstreamReserve(repo.Branches["origin/infrastructure"].Tip.Sha)), ("feature-a", new UpstreamReserve(repo.Branches["origin/feature-a"].Tip.Sha))),
                    IncludedBranches(
                        ("origin/feature-b", new BranchReserveBranch(repo.Branches["origin/feature-b"].Tip.Sha, Metadata(("Role", "Output"))))
                    ),
                    outputCommit: repo.Branches["origin/feature-b"].Tip.Sha,
                    meta: Metadata()) },
                { "feature-a", new BranchReserve("dontcare", "Automatic", "Stable",
                    Upstream(),
                    IncludedBranches(
                        ("origin/feature-a", new BranchReserveBranch(repo.Branches["origin/feature-a"].Tip.Sha, Metadata(("Role", "Output"))))
                    ),
                    outputCommit: repo.Branches["origin/feature-a"].Tip.Sha,
                    meta: Metadata()) },
                { "infrastructure", new BranchReserve("dontcare", "dontcare", "Stable",
                    Upstream(),
                    IncludedBranches(
                        ("origin/infrastructure", new BranchReserveBranch(repo.Branches["origin/infrastructure"].Tip.Sha, Metadata(("Role", "Output"))))
                    ),
                    outputCommit: repo.Branches["origin/infrastructure"].Tip.Sha,
                    meta: Metadata()) }
            });

            // Assert that we're correct
            using var targetRepo = new Repository(gitDir.Path);

            Assert.Collection(result,
                // create reserve for a/infrastructure conflict
                standardAction =>
                {
                    var action = Assert.IsType<CreateReserveAction>(standardAction.Payload);
                    Assert.Equal("Automatic", action.FlowType);
                    Assert.Collection(action.Upstream, r => Assert.Equal("feature-a", r), r => Assert.Equal("infrastructure", r));
                    Assert.Equal("integration", action.Type);
                    Assert.Equal("integ/feature-a_infrastructure", action.Name);
                    Assert.Null(action.OriginalBranch);
                },
                // add reserve to feature-b
                standardAction =>
                {
                    var action = Assert.IsType<AddUpstreamReserveAction>(standardAction.Payload);
                    Assert.Equal("feature-b", action.Target);
                    Assert.Equal("integ/feature-a_infrastructure", action.Upstream);
                    Assert.Null(action.Role);
                    Assert.Null(action.Meta);
                },
                standardAction =>
                {
                    // finalize feature-b status
                    var action = Assert.IsType<ManualInterventionNeededAction>(standardAction.Payload);
                    Assert.Empty(action.BranchCommits);
                    Assert.Empty(action.ReserveOutputCommits);
                    Assert.Empty(action.NewBranches);
                    Assert.Equal("OutOfDate", action.State);
                    Assert.Equal("feature-b", action.Reserve);
                }
            );
        }
        // TODO - more tests

        private ImmutableSortedDictionary<string, BranchReserveBranch> IncludedBranches(params (string key, BranchReserveBranch value)[] upstream)
        {
            return upstream.ToImmutableSortedDictionary(p => p.key, p => p.value);
        }

        private ImmutableSortedDictionary<string, UpstreamReserve> Upstream(params (string key, UpstreamReserve value)[] upstream)
        {
            return upstream.ToImmutableSortedDictionary(p => p.key, p => p.value);
        }

        private ImmutableSortedDictionary<string, string> Metadata(params (string key, string value)[] metadata)
        {
            return metadata.ToImmutableSortedDictionary(p => p.key, p => p.value);
        }

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

        private static AutomationOptions AutomationOptions()
        {
            return new AutomationOptions
            {
                WorkerCount = 1,
                WorkspacePath = "NA",
                WorkingRemote = "local",
                DefaultRemote = "origin",
                IntegrationPrefix = "merge/",
            };
        }

        private async Task<IList<StateUpdateEvent<IStandardAction>>> Invoke(TemporaryDirectory repository, TemporaryDirectory tempPath, GitDirectory checkout, string reserveName, Dictionary<string, BranchReserve> reserves)
        {
            using var repo = new Repository(checkout.Path);
            var resultList = new List<StateUpdateEvent<IStandardAction>>();
            var script = new HandleOutOfDateIntegrationBranchScript(
                new DispatchToList(resultList),
                Options.Create(StandardParameters(repository, checkout.TemporaryDirectory)),
                Options.Create(AutomationOptions()),
                new DefaultBranchNaming(Options.Create(AutomationOptions())),
                new IntegrationReserveUtilities()
            );
            var reserve = reserves[reserveName];
            using var loggerFactory = LoggerFactory.Create(_ => { });
            await script.Run(
                new ReserveScriptParameters(reserveName, 
                    reserve,
                    reserve.IncludedBranches.Keys.ToImmutableDictionary(k => k, k => repo.Branches[k] != null ? repo.Branches[k].Tip.Sha : BranchReserve.EmptyCommit),
                    reserve.Upstream.Keys.ToImmutableDictionary(k => k, upstream => reserves.ContainsKey(upstream) ? reserves[upstream] : throw new InvalidOperationException($"Unknown upstream: {upstream}")), 
                    tempPath.Path),
                loggerFactory.CreateLogger(this.GetType().FullName),
                SystemAgent.Instance
            );
            return resultList;
        }
    }
}
