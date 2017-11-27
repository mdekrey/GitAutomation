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

            public readonly Mock<IGitServiceApi> gitServiceApiMock = new Mock<IGitServiceApi>();
            public readonly Mock<IUnitOfWorkFactory> workFactoryMock = new Mock<IUnitOfWorkFactory>() { DefaultValue = DefaultValue.Mock };
            public readonly Mock<IRepositoryOrchestration> orchestrationMock = new Mock<IRepositoryOrchestration>();
            public readonly Mock<IBranchSettings> settingsMock = new Mock<IBranchSettings>();
            public readonly Mock<IRepositoryMediator> repositoryMediatorMock = new Mock<IRepositoryMediator>();
            public readonly Mock<IRemoteRepositoryState> repositoryMock = new Mock<IRemoteRepositoryState>();

            public IntegrateBranchesOrchestration Target { get; }
            public Mock<IMergeDelegate> AttemptMergeMock { get; }
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

            Assert.IsFalse(result.HadPullRequest);
            Assert.IsTrue(result.Conflicts.Any(a => a.BranchA.GroupName == "A" && a.BranchB.GroupName == "B"));
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

            Assert.IsFalse(result.HadPullRequest);
            Assert.IsTrue(result.Conflicts.Any(a => a.BranchA.GroupName == "A" && a.BranchB.GroupName == "B"));
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
            var serviceLineGroups = Observable.Return(new [] { new BranchGroup { GroupName = "ServiceLine", BranchType = BranchGroupType.ServiceLine, RecreateFromUpstream = false } }.ToImmutableList());
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("A")).Returns(serviceLineGroups);
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("B")).Returns(serviceLineGroups);
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("ServiceLine")).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "B", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "ServiceLine", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("B", "ServiceLine", It.IsAny<string>())).Returns(Task.FromResult(true));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("A", "C", It.IsAny<string>())).Returns(Task.FromResult(false));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("B", "C", It.IsAny<string>())).Returns(Task.FromResult(false));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("C", "ServiceLine", It.IsAny<string>())).Returns(Task.FromResult(false));

            var result = await setup.Target.FindConflicts("C", new[] { "A", "B" }, setup.AttemptMergeMock.Object.AttemptMergeDelegate);

            Assert.IsFalse(result.HadPullRequest);
            Assert.IsTrue(result.Conflicts.Any(a => a.BranchA.GroupName == "C" && a.BranchB.GroupName == "ServiceLine"));
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
                new [] { new BranchGroup { GroupName = "ServiceLine", BranchType = BranchGroupType.ServiceLine, RecreateFromUpstream = false } }.ToImmutableList()
            );
            setup.settingsMock.Setup(settings => settings.GetUpstreamBranches("ServiceLine")).Returns(Observable.Return(ImmutableList<BranchGroup>.Empty));
            setup.AttemptMergeMock.Setup(t => t.AttemptMergeDelegate("B", "ServiceLine", It.IsAny<string>())).Returns(Task.FromResult(false));
            setup.settingsMock.Setup(settings => settings.GetIntegrationBranch("B", "ServiceLine")).ReturnsAsync("Integ");
            setup.settingsMock.Setup(settings => settings.AddBranchPropagation("Integ", "B", It.IsAny<IUnitOfWork>())).Verifiable();
            setup.settingsMock.Setup(settings => settings.RemoveBranchPropagation("B", "Integ", It.IsAny<IUnitOfWork>())).Verifiable();
            setup.orchestrationMock.Setup(orchestration => orchestration.EnqueueAction(It.Is<MergeDownstreamAction>(a => a.DownstreamBranch == "B"), false)).Verifiable();
            setup.orchestrationMock.Setup(orchestration => orchestration.EnqueueAction(It.Is<ConsolidateMergedAction>(a => a.NewBaseBranch == "B" && a.OriginalBranches.Single() == "Integ"), false)).Verifiable();

            var result = await setup.Target.FindAndCreateIntegrationBranches(new BranchGroupCompleteData
            {
                GroupName = "B",
                LatestBranchName = "B",
                UpstreamBranchGroups = new[] { "ServiceLine" }.ToImmutableList()
            }, new[] { "ServiceLine" }, setup.AttemptMergeMock.Object.AttemptMergeDelegate);

            Assert.IsFalse(result.HadPullRequest);
            Assert.IsTrue(result.AddedNewIntegrationBranches);
            Assert.IsTrue(result.Conflicts.Any(a => a.BranchA.GroupName == "B" && a.BranchB.GroupName == "ServiceLine"));
            setup.settingsMock.Verify(settings => settings.AddBranchPropagation("Integ", "B", It.IsAny<IUnitOfWork>()), Times.Once());
            setup.settingsMock.Verify(settings => settings.RemoveBranchPropagation("B", "Integ", It.IsAny<IUnitOfWork>()), Times.Once());
            setup.orchestrationMock.Verify(orchestration => orchestration.EnqueueAction(It.Is<MergeDownstreamAction>(a => a.DownstreamBranch == "B"), false), Times.Once());
            setup.orchestrationMock.Verify(orchestration => orchestration.EnqueueAction(It.Is<ConsolidateMergedAction>(a => a.NewBaseBranch == "B" && a.OriginalBranches.Single() == "Integ"), false), Times.Once());
        }
    }
}
