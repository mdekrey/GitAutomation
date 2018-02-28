using GitAutomation.BranchSettings;
using GitAutomation.GitService;
using GitAutomation.Repository;
using GitAutomation.Work;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Orchestration.Actions
{
    [TestClass]
    public class IntegrateBranchesOrchestrationShould
    {
        public interface IMergeDelegate
        {
            Task<bool> AttemptMergeDelegate(string upstreamBranch, string targetBranch, string message);
        }

        class BranchSetup
        {
            public string Name { get; set; }
            public string Commit { get; set; }
            public string[] Downstream { get; set; } = Array.Empty<string>();
            public string[] ConflictsWith { get; set; } = Array.Empty<string>();
            public string[] PullRequestsFrom { get; set; } = Array.Empty<string>();

            public BranchSetup GenerateCommitIfEmpty()
            {
                Commit = Commit ?? Name.GetHashCode().ToString("X");
                return this;
            }

            internal GitRef ToGitRef() => new GitRef { Name = Name, Commit = Commit };
            internal BranchGroup ToBranchGroup() => new BranchGroup { GroupName = Name, BranchType = BranchGroupType.Feature, UpstreamMergePolicy = UpstreamMergePolicy.None };

            internal IEnumerable<PullRequest> ToPullRequests() =>
                from source in PullRequestsFrom
                select new PullRequest
                {
                    Author = "someone",
                    SourceBranch = source,
                    TargetBranch = Name,
                    State = PullRequestState.Open,
                    Reviews = ImmutableList<PullRequestReview>.Empty
                };
        }

        class Setup
        {
            public Setup()
            {
                var branchNaming = new HyphenSuffixIterationNaming();
                Target = new IntegrateBranchesOrchestration(
                    gitServiceApiMock.Object,
                    workFactoryMock.Object,
                    orchestrationMock.Object,
                    new IntegrationNamingMediator(new StandardIntegrationNamingConvention(branchNaming), repositoryMock.Object),
                    settingsMock.Object,
                    repositoryMediatorMock.Object,
                    new BranchIterationMediator(branchNaming, repositoryMock.Object)
                );
                AttemptMergeMock = new Mock<IMergeDelegate>();
            }

            public Setup(BranchSetup[] branches)
                : this()
            {
                var branchByName = branches.ToDictionary(b => b.Name, b => b.GenerateCommitIfEmpty());
                var connections = (from b in branchByName.Values
                                   from downstream in b.Downstream
                                   select (upstream: b.Name, downstream)).ToArray();
                var prs = (from b in branchByName.Values
                           from pr in b.ToPullRequests()
                           select pr).ToArray();
                repositoryMediatorMock.Setup(repository => repository.GetAllBranchRefs()).Returns(Observable.Return(
                    branchByName.Values.Select(b => b.ToGitRef()).ToImmutableList()
                ));
                gitServiceApiMock.Setup(git => git.GetPullRequests(PullRequestState.Open, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PullRequestAuthorMode>()))
                    .Returns<PullRequestState, string, string, bool, PullRequestAuthorMode>((state, target, source, includeReviews, authorMode) =>
                    {
                        return Task.FromResult((from pr in prs
                                                where pr.TargetBranch == target || target == null
                                                where pr.SourceBranch == source || source == null
                                                select pr).ToImmutableList());
                    });
                settingsMock.Setup(settings => settings.GetDownstreamBranches(It.IsAny<string>())).Returns<string>(branchName => 
                    Observable.Return(branchByName[branchName].Downstream.Select(b => branchByName[b].ToBranchGroup()).ToImmutableList())
                );
                settingsMock.Setup(settings => settings.GetUpstreamBranches(It.IsAny<string>())).Returns<string>(branchName =>
                    Observable.Return((from b in branchByName.Values
                                       where b.Downstream.Contains(branchName)
                                       select b.ToBranchGroup()).ToImmutableList())
                );
                settingsMock.Setup(settings => settings.GetAllUpstreamBranches(It.IsAny<string>())).Returns<string>(branchName => 
                    Observable.Return(GetAllUpstreamFrom(branchName, connections).Select(b => branchByName[b].ToBranchGroup()).ToImmutableList())
                );
                settingsMock.Setup(settings => settings.GetAllDownstreamBranches(It.IsAny<string>())).Returns<string>(branchName =>
                    Observable.Return(GetAllDownstreamFrom(branchName, connections).Select(b => branchByName[b].ToBranchGroup()).ToImmutableList())
                );
                AttemptMergeMock.Setup(t => t.AttemptMergeDelegate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns<string, string, string>((a, b, message) =>
                    Task.FromResult(branchByName[a].ConflictsWith.Contains(b) || branchByName[b].ConflictsWith.Contains(a))
                );

            }

            public readonly Mock<IGitServiceApi> gitServiceApiMock = new Mock<IGitServiceApi>();
            public readonly Mock<IUnitOfWorkFactory> workFactoryMock = new Mock<IUnitOfWorkFactory>() { DefaultValue = DefaultValue.Mock };
            public readonly Mock<IRepositoryOrchestration> orchestrationMock = new Mock<IRepositoryOrchestration>();
            public readonly Mock<IBranchSettings> settingsMock = new Mock<IBranchSettings>();
            public readonly Mock<IRepositoryMediator> repositoryMediatorMock = new Mock<IRepositoryMediator>();
            public readonly Mock<IRemoteRepositoryState> repositoryMock = new Mock<IRemoteRepositoryState>();

            public IntegrateBranchesOrchestration Target { get; }
            public Mock<IMergeDelegate> AttemptMergeMock { get; }


            private static ImmutableHashSet<string> GetAllDownstreamFrom(string targetBranch, (string upstream, string downstream)[] connections)
            {
                var result = new HashSet<string>();
                var queue = new Queue<string>(new[] { targetBranch });

                while (queue.Any())
                {
                    var current = queue.Dequeue();
                    if (result.Contains(current))
                    {
                        continue;
                    }
                    result.Add(current);
                    foreach (var connection in connections.Where(s => s.upstream == current))
                    {
                        queue.Enqueue(connection.downstream);
                    }
                }
                return result.Except(new[] { targetBranch }).ToImmutableHashSet();
            }

            private static ImmutableHashSet<string> GetAllUpstreamFrom(string targetBranch, (string upstream, string downstream)[] connections)
            {
                var result = new HashSet<string>();
                var queue = new Queue<string>(new[] { targetBranch });

                while (queue.Any())
                {
                    var current = queue.Dequeue();
                    if (result.Contains(current))
                    {
                        continue;
                    }
                    result.Add(current);
                    foreach (var connection in connections.Where(s => s.downstream == current))
                    {
                        queue.Enqueue(connection.upstream);
                    }
                }
                return result.Except(new[] { targetBranch }).ToImmutableHashSet();
            }
        }

        [TestMethod]
        public async Task FindParentConflictsOnCreate()
        {
            var setup = new Setup();
            setup.repositoryMediatorMock.Setup(repository => repository.GetAllBranchRefs()).Returns(Observable.Return(
                new[]
                {
                    new GitRef
                    {
                        Name = "A",
                        Commit = "0000000"
                    },
                    new GitRef
                    {
                        Name = "B",
                        Commit = "1111111"
                    },
                }.ToImmutableList()
            ));
            setup.gitServiceApiMock.Setup(git => git.GetPullRequests(It.IsAny<PullRequestState?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PullRequestAuthorMode>())).Returns(Task.FromResult(ImmutableList<PullRequest>.Empty));
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches(It.IsAny<string>())).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "B", It.IsAny<string>())).Returns(Task.FromResult(false));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "C", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("B", "C", It.IsAny<string>())).Returns(Task.FromResult(false));

            var result = await setup.Target.FindConflicts("C", new[] { "A", "B" }, setup.AttemptMergeMock.Object.AttemptMergeDelegate);

            Assert.IsNotNull(result.Conflicts.SingleOrDefault(a => a.BranchA.GroupName == "A" && a.BranchB.GroupName == "B"));
        }

        [TestMethod]
        public async Task FindParentConflicts()
        {
            var setup = new Setup();
            setup.repositoryMediatorMock.Setup(repository => repository.GetAllBranchRefs()).Returns(Observable.Return(
                new[]
                {
                    new GitRef
                    {
                        Name = "A",
                        Commit = "0000000"
                    },
                    new GitRef
                    {
                        Name = "B",
                        Commit = "1111111"
                    },
                    new GitRef
                    {
                        Name = "C",
                        Commit = "2222222"
                    },
                }.ToImmutableList()
            ));
            setup.gitServiceApiMock.Setup(git => git.GetPullRequests(It.IsAny<PullRequestState?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PullRequestAuthorMode>())).Returns(Task.FromResult(ImmutableList<PullRequest>.Empty));
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches(It.IsAny<string>())).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "B", It.IsAny<string>())).Returns(Task.FromResult(false));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "C", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("B", "C", It.IsAny<string>())).Returns(Task.FromResult(false));

            var result = await setup.Target.FindConflicts("C", new[] { "A", "B" }, setup.AttemptMergeMock.Object.AttemptMergeDelegate);

            Assert.IsNotNull(result.Conflicts.SingleOrDefault(a => a.BranchA.GroupName == "A" && a.BranchB.GroupName == "B"));
        }

        [TestMethod]
        public async Task FindParentConflictsIssue76()
        {
            var setup = new Setup();
            setup.repositoryMediatorMock.Setup(repository => repository.GetAllBranchRefs()).Returns(Observable.Return(
                new[]
                {
                    new GitRef
                    {
                        Name = "A",
                        Commit = "0000000"
                    },
                    new GitRef
                    {
                        Name = "B",
                        Commit = "1111111"
                    },
                    new GitRef
                    {
                        Name = "C",
                        Commit = "2222222"
                    },
                    new GitRef
                    {
                        Name = "ServiceLine",
                        Commit = "3333333"
                    },
                }.ToImmutableList()
            ));
            setup.gitServiceApiMock.Setup(git => git.GetPullRequests(It.IsAny<PullRequestState?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PullRequestAuthorMode>())).Returns(Task.FromResult(ImmutableList<PullRequest>.Empty));
            var serviceLineGroups = Observable.Return(new [] { new BranchGroup { GroupName = "ServiceLine", BranchType = BranchGroupType.ServiceLine, UpstreamMergePolicy = UpstreamMergePolicy.None } }.ToImmutableList());
            var cUpstreamGroups = Observable.Return(new [] { new BranchGroup { GroupName = "ServiceLine", BranchType = BranchGroupType.ServiceLine, UpstreamMergePolicy = UpstreamMergePolicy.None } }.ToImmutableList());
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("C")).Returns(cUpstreamGroups);
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("A")).Returns(serviceLineGroups);
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("B")).Returns(serviceLineGroups);
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("ServiceLine")).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.settingsMock.Setup(settings => settings.GetAllUpstreamBranches("C")).Returns(serviceLineGroups.CombineLatest(cUpstreamGroups, (a, b) => a.Concat(b).ToImmutableList()));
            setup.settingsMock.Setup(settings => settings.GetAllUpstreamBranches("A")).Returns(serviceLineGroups);
            setup.settingsMock.Setup(settings => settings.GetAllUpstreamBranches("B")).Returns(serviceLineGroups);
            setup.settingsMock.Setup(settings => settings.GetAllUpstreamBranches("ServiceLine")).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "B", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "ServiceLine", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("B", "ServiceLine", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "C", It.IsAny<string>())).Returns(Task.FromResult(false));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("B", "C", It.IsAny<string>())).Returns(Task.FromResult(false));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("C", "ServiceLine", It.IsAny<string>())).Returns(Task.FromResult(false));

            var result = await setup.Target.FindConflicts("C", new[] { "A", "B" }, setup.AttemptMergeMock.Object.AttemptMergeDelegate);

            Assert.IsNotNull(result.Conflicts.SingleOrDefault(a => a.BranchA.GroupName == "C" && a.BranchB.GroupName == "ServiceLine"));
        }

        [TestMethod]
        public async Task FindIntegrationFromConsolidationIssue75()
        {
            var setup = new Setup();
            setup.repositoryMediatorMock.Setup(repository => repository.GetAllBranchRefs()).Returns(Observable.Return(
                new[]
                {
                    new GitRef
                    {
                        Name = "Integ",
                        Commit = "0000000"
                    },
                    new GitRef
                    {
                        Name = "B",
                        Commit = "1111111"
                    },
                    new GitRef
                    {
                        Name = "ServiceLine",
                        Commit = "2222222"
                    },
                }.ToImmutableList()
            ));
            setup.gitServiceApiMock.Setup(git => git.GetPullRequests(It.IsAny<PullRequestState?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PullRequestAuthorMode>())).Returns(Task.FromResult(ImmutableList<PullRequest>.Empty));
            var serviceLineGroups = Observable.Return(
                new [] { new BranchGroup { GroupName = "ServiceLine", BranchType = BranchGroupType.ServiceLine, UpstreamMergePolicy = UpstreamMergePolicy.None } }.ToImmutableList()
            );
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("B")).Returns(serviceLineGroups);
            setup.settingsMock.Setup(settings => settings.GetAllUpstreamBranches("B")).Returns(serviceLineGroups);
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("ServiceLine")).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.settingsMock.Setup(settings => settings.GetAllUpstreamBranches("ServiceLine")).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("B", "ServiceLine", It.IsAny<string>())).Returns(Task.FromResult(false));
            setup.settingsMock.Setup(settings => settings.FindIntegrationBranchForConflict("B", "ServiceLine", It.IsAny<ImmutableList<string>>())).ReturnsAsync("Integ");
            setup.settingsMock.Setup(settings => settings.AddBranchPropagation("Integ", "B", It.IsAny<IUnitOfWork>())).Verifiable();
            setup.settingsMock.Setup(settings => settings.RemoveBranchPropagation("B", "Integ", It.IsAny<IUnitOfWork>())).Verifiable();
            setup.orchestrationMock.Setup(orchestration => orchestration.EnqueueAction(It.Is<MergeDownstreamAction>(a => a.DownstreamBranch == "B"), false)).Verifiable();
            setup.orchestrationMock.Setup(orchestration => orchestration.EnqueueAction(It.Is<ConsolidateMergedAction>(a => a.NewBaseBranch == "B" && a.SourceBranch == "Integ"), false)).Verifiable();

            var result = await setup.Target.FindAndCreateIntegrationBranches(new BranchGroupCompleteData
            {
                GroupName = "B",
                LatestBranchName = "B",
                UpstreamBranchGroups = new[] { "ServiceLine" }.ToImmutableList()
            }, new[] { "ServiceLine" }, setup.AttemptMergeMock.Object.AttemptMergeDelegate);

            Assert.IsTrue(result.AddedNewIntegrationBranches);
            Assert.IsTrue(result.Conflicts.Any(a => a.BranchA.GroupName == "B" && a.BranchB.GroupName == "ServiceLine"));
            setup.settingsMock.Verify(settings => settings.AddBranchPropagation("Integ", "B", It.IsAny<IUnitOfWork>()), Times.Once());
            setup.settingsMock.Verify(settings => settings.RemoveBranchPropagation("B", "Integ", It.IsAny<IUnitOfWork>()), Times.Once());
            setup.orchestrationMock.Verify(orchestration => orchestration.EnqueueAction(It.Is<MergeDownstreamAction>(a => a.DownstreamBranch == "B"), false), Times.Once());
            setup.orchestrationMock.Verify(orchestration => orchestration.EnqueueAction(It.Is<ConsolidateMergedAction>(a => a.NewBaseBranch == "B" && a.SourceBranch == "Integ"), false), Times.Once());
        }

        [TestMethod]
        public async Task FindIntegrationFromConsolidationIssue119WithBaseIntegration()
        {
            var setup = new Setup();
            setup.repositoryMediatorMock.Setup(repository => repository.GetAllBranchRefs()).Returns(Observable.Return(
                new[]
                {
                    new GitRef
                    {
                        Name = "A",
                        Commit = "0000000"
                    },
                    new GitRef
                    {
                        Name = "B",
                        Commit = "1111111"
                    },
                    new GitRef
                    {
                        Name = "Integ-AB",
                        Commit = "0101010"
                    },
                    new GitRef
                    {
                        Name = "C-from-A",
                        Commit = "0002222"
                    },
                }.ToImmutableList()
            ));
            setup.gitServiceApiMock.Setup(git => git.GetPullRequests(It.IsAny<PullRequestState?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PullRequestAuthorMode>())).Returns(Task.FromResult(ImmutableList<PullRequest>.Empty));

            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("A")).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("B")).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("C-from-A")).Returns(Observable.Return(new[] {
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "A",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                }
            }.ToImmutableList()));
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("Integ-AB")).Returns(Observable.Return(new[] {
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "A",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                },
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "B",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                }
            }.ToImmutableList()));
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("Result")).Returns(Observable.Return(new[] {
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "A",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                },
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "B",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                },
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "Integ-AB",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                },
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "C-from-A",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                }
            }.ToImmutableList()));

            setup.settingsMock.Setup(settings => settings.GetAllUpstreamBranches("A")).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.settingsMock.Setup(settings => settings.GetAllUpstreamBranches("B")).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.settingsMock.Setup(settings => settings.GetAllUpstreamBranches("C-from-A")).Returns(Observable.Return(new[] {
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "A",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                }
            }.ToImmutableList()));
            setup.settingsMock.Setup(settings => settings.GetAllUpstreamBranches("Integ-AB")).Returns(Observable.Return(new[] {
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "A",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                },
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "B",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                }
            }.ToImmutableList()));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "B", It.IsAny<string>())).Returns(Task.FromResult(false));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "C-from-A", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "Integ-AB", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("B", "Integ-AB", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("B", "C-from-A", It.IsAny<string>())).Returns(Task.FromResult(false));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("C-from-A", "Integ-AB", It.IsAny<string>())).Returns(Task.FromResult(true));

            var result = await setup.Target.FindConflicts("Result", new[] { "C-from-A", "B", "A", "Integ-AB" }, setup.AttemptMergeMock.Object.AttemptMergeDelegate);

            Assert.IsFalse(result.Conflicts.Any());
            Assert.IsFalse(result.AddedNewIntegrationBranches);
        }

        [TestMethod]
        public async Task FindIntegrationFromConsolidationIssue119WithoutBaseIntegration()
        {
            var setup = new Setup();
            setup.repositoryMediatorMock.Setup(repository => repository.GetAllBranchRefs()).Returns(Observable.Return(
                new[]
                {
                    new GitRef
                    {
                        Name = "A",
                        Commit = "0000000"
                    },
                    new GitRef
                    {
                        Name = "B",
                        Commit = "1111111"
                    },
                    new GitRef
                    {
                        Name = "Integ-AB",
                        Commit = "0101010"
                    },
                    new GitRef
                    {
                        Name = "C-from-A",
                        Commit = "0002222"
                    },
                }.ToImmutableList()
            ));
            setup.gitServiceApiMock.Setup(git => git.GetPullRequests(It.IsAny<PullRequestState?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PullRequestAuthorMode>())).Returns(Task.FromResult(ImmutableList<PullRequest>.Empty));

            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("A")).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("B")).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("C-from-A")).Returns(Observable.Return(new[] {
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "A",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                }
            }.ToImmutableList()));
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("Integ-AB")).Returns(Observable.Return(new[] {
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "A",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                },
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "B",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                }
            }.ToImmutableList()));
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("Result")).Returns(Observable.Return(new[] {
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "A",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                },
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "B",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                },
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "C-from-A",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                }
            }.ToImmutableList()));

            setup.settingsMock.Setup(settings => settings.GetAllUpstreamBranches("A")).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.settingsMock.Setup(settings => settings.GetAllUpstreamBranches("B")).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.settingsMock.Setup(settings => settings.GetAllUpstreamBranches("C-from-A")).Returns(Observable.Return(new[] {
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "A",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                }
            }.ToImmutableList()));
            setup.settingsMock.Setup(settings => settings.GetAllUpstreamBranches("Integ-AB")).Returns(Observable.Return(new[] {
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "A",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                },
                new BranchGroup
                {
                    BranchType = BranchGroupType.Feature,
                    GroupName = "B",
                    UpstreamMergePolicy = UpstreamMergePolicy.None
                }
            }.ToImmutableList()));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "B", It.IsAny<string>())).Returns(Task.FromResult(false));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "C-from-A", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "Integ-AB", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("B", "Integ-AB", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("B", "C-from-A", It.IsAny<string>())).Returns(Task.FromResult(false));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("C-from-A", "Integ-AB", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.settingsMock.Setup(settings => settings.FindIntegrationBranchForConflict("A", "B", It.IsAny<ImmutableList<string>>())).ReturnsAsync("Integ-AB");

            var result = await setup.Target.FindAndCreateIntegrationBranches(new BranchGroupCompleteData
            {
                GroupName = "Result",
                LatestBranchName = "Result",
                UpstreamBranchGroups = new[] { "A", "B", "C-from-A" }.ToImmutableList()
            }, new[] { "A", "B", "C-from-A" }, setup.AttemptMergeMock.Object.AttemptMergeDelegate);

            Assert.AreEqual("A", result.Conflicts.Single().BranchA.GroupName);
            Assert.AreEqual("B", result.Conflicts.Single().BranchB.GroupName);
            Assert.IsTrue(result.AddedNewIntegrationBranches);
        }

        [TestMethod]
        public async Task BlockBadBranchConflictIssue126()
        {
            var setup = new Setup(new[] {
                new BranchSetup { Name = "A", Downstream = new[] { "B", "C" } },
                new BranchSetup { Name = "B", ConflictsWith = new[] { "C", "D", "E" }, Downstream = new[] { "RC" }  },
                new BranchSetup { Name = "C", Downstream = new[] { "D", "E" }, PullRequestsFrom = new[] { "F" } },
                new BranchSetup { Name = "D", Downstream = new[] { "RC" } },
                new BranchSetup { Name = "E", Downstream = new[] { "RC" } },
                new BranchSetup { Name = "F" },
                new BranchSetup { Name = "RC" },
            });

            var result = await setup.Target.FindConflicts("RC", null, setup.AttemptMergeMock.Object.AttemptMergeDelegate);

            Assert.IsNotNull(result.Conflicts.SingleOrDefault(a => a.BranchA.GroupName == "B" && a.BranchB.GroupName == "C"));
        }

    }
}
