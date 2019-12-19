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
    public class HandleNeedsUpdateScriptShould
    {
        private readonly GitDirectory workingGitDirectory;

        public HandleNeedsUpdateScriptShould(BranchGitDirectory workingGitDirectory)
        {
            this.workingGitDirectory = workingGitDirectory;
            using var newRepo = new Repository(workingGitDirectory.Path);
        }

        [Fact]
        public async Task HandleMergeCompleted()
        {
            using var gitDir = new GitDirectory(isBare: true);
            using var checkout = workingGitDirectory.CreateCopy(new CloneOptions { IsBare = true });
            using var tempDir = new TemporaryDirectory();
            
            using var originalRepo = new Repository(gitDir.Path);
            using var repo = new Repository(checkout.Path);
            var originalMaster = repo.Branches["origin/master"].Tip.Sha;
            var originalFeatureA = repo.Branches["origin/feature-a"].Tip.Sha;
            repo.Branches.Add("origin/merge/master_feature-a", originalMaster);
            repo.Network.Remotes.Add("origin", gitDir.Path);
            repo.Network.Push(repo.Network.Remotes["origin"], repo.Branches.Select(b => $"{b.CanonicalName}:{b.CanonicalName.Replace("origin/", "")}"));

            // Act to receive the expected FSA's
            var result = await Invoke(gitDir.TemporaryDirectory, tempDir, checkout, "feature-a", new Dictionary<string, BranchReserve>
            {
                { "feature-a", new BranchReserve("dontcare", "Manual", "NeedsUpdate",
                    Upstream(("master", new UpstreamReserve(originalMaster))),
                    IncludedBranches(
                        ("origin/merge/master_feature-a", new BranchReserveBranch(originalMaster, Metadata(("Role", "Integration"), ("Source", "master")))),
                        ("origin/feature-a", new BranchReserveBranch(originalFeatureA, Metadata(("Role", "Output"))))
                    ),
                    outputCommit: originalFeatureA,
                    meta: Metadata()) },
                { "master", new BranchReserve("dontcare", "dontcare", "Stable",
                    Upstream(),
                    IncludedBranches(
                        ("origin/master", new BranchReserveBranch(originalMaster, Metadata(("Role", "Output"))))
                    ),
                    outputCommit: originalMaster,
                    meta: Metadata()) }
            });

            // Assert that we're correct
            using var targetRepo = new Repository(gitDir.Path);

            Assert.Collection(result,
                standardAction =>
                {
                    var action = Assert.IsType<DeleteBranchAction>(standardAction.Payload);
                    Assert.Equal("origin/merge/master_feature-a", action.TargetBranch);
                },
                standardAction =>
                {
                    var action = Assert.IsType<StabilizeReserveAction>(standardAction.Payload);
                    Assert.Equal("feature-a", action.Reserve);
                });

            Assert.Null(originalRepo.Branches["merge/master_feature-a"]);
        }

        [Fact]
        public async Task HandleMergePendingNoUpdate()
        {
            using var gitDir = new GitDirectory(isBare: true);
            using var checkout = workingGitDirectory.CreateCopy(new CloneOptions { IsBare = true });
            using var tempDir = new TemporaryDirectory();

            using var repo = new Repository(checkout.Path);
            var originalMaster = repo.Branches["origin/master"].Tip.Sha;
            var originalFeatureA = repo.Branches["origin/feature-a"].Tip.Sha;
            var originalState = "NeedsUpdate";
            repo.Branches.Add("origin/merge/feature-a_master", originalFeatureA);

            // Act to receive the expected FSA's
            var result = await Invoke(gitDir.TemporaryDirectory, tempDir, checkout, "master", new Dictionary<string, BranchReserve>
            {
                { "feature-a", new BranchReserve("dontcare", "dontcare", "Stable",
                    Upstream(("master", new UpstreamReserve(originalMaster))),
                    IncludedBranches(
                        ("origin/feature-a", new BranchReserveBranch(originalFeatureA, Metadata(("Role", "Output"))))
                    ),
                    outputCommit: originalFeatureA,
                    meta: Metadata()) },
                { "master", new BranchReserve("dontcare", "Manual", originalState,
                    Upstream(),
                    IncludedBranches(
                        ("origin/merge/feature-a_master", new BranchReserveBranch(originalFeatureA, Metadata(("Role", "Integration"), ("Source", "feature-a")))),
                        ("origin/master", new BranchReserveBranch(originalMaster, Metadata(("Role", "Output"))))
                    ),
                    outputCommit: originalMaster,
                    meta: Metadata()) }
            });

            // Assert that we're correct
            using var targetRepo = new Repository(gitDir.Path);

            Assert.Collection(result,
                standardAction =>
                {
                    var action = Assert.IsType<ManualInterventionNeededAction>(standardAction.Payload);
                    Assert.Equal("master", action.Reserve);
                    Assert.Equal(originalState, action.State);
                    Assert.Empty(action.BranchCommits);
                    Assert.Empty(action.NewBranches);
                });
        }

        [Fact]
        public async Task HandleMergePendingWithUpdate()
        {
            using var gitDir = new GitDirectory(isBare: true);
            using var checkout = workingGitDirectory.CreateCopy(new CloneOptions { IsBare = true });
            using var tempDir = new TemporaryDirectory();

            using var repo = new Repository(checkout.Path);
            var originalMaster = repo.Branches["origin/master"].Tip.Sha;
            var originalFeatureA = repo.Branches["origin/feature-a"].Tip.Sha;
            var originalState = "NeedsUpdate";
            repo.Branches.Add("origin/merge/feature-a_master", originalFeatureA);

            // Act to receive the expected FSA's
            var result = await Invoke(gitDir.TemporaryDirectory, tempDir, checkout, "master", new Dictionary<string, BranchReserve>
            {
                { "feature-a", new BranchReserve("dontcare", "dontcare", "Stable",
                    Upstream(("master", new UpstreamReserve(originalMaster))),
                    IncludedBranches(
                        ("origin/feature-a", new BranchReserveBranch(originalFeatureA, Metadata(("Role", "Output"))))
                    ),
                    outputCommit: originalFeatureA,
                    meta: Metadata()) },
                { "master", new BranchReserve("dontcare", "Manual", originalState,
                    Upstream(),
                    IncludedBranches(
                        ("origin/merge/feature-a_master", new BranchReserveBranch(BranchReserve.EmptyCommit, Metadata(("Role", "Integration"), ("Source", "feature-a")))),
                        ("origin/master", new BranchReserveBranch(originalMaster, Metadata(("Role", "Output"))))
                    ),
                    outputCommit: originalMaster,
                    meta: Metadata()) }
            });

            // Assert that we're correct
            using var targetRepo = new Repository(gitDir.Path);

            Assert.Collection(result,
                standardAction =>
                {
                    var action = Assert.IsType<ManualInterventionNeededAction>(standardAction.Payload);
                    Assert.Equal("master", action.Reserve);
                    Assert.Equal(originalState, action.State);
                    Assert.Collection(action.BranchCommits,
                        kvp =>
                        {
                            Assert.Equal("origin/merge/feature-a_master", kvp.Key);
                            Assert.Equal(originalFeatureA, kvp.Value);
                        });
                    Assert.Empty(action.NewBranches);
                });
        }

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
            var script = new HandleNeedsUpdateScript(
                new DispatchToList(resultList),
                Options.Create(StandardParameters(repository, checkout.TemporaryDirectory)),
                Options.Create(AutomationOptions()),
                new DefaultBranchNaming(Options.Create(AutomationOptions()))
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
