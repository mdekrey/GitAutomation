using GitAutomation.BranchSettings;
using GitAutomation.Processes;
using GitAutomation.Repository;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Orchestration.Actions.MergeStrategies
{
    class ForceFreshMergeStrategy : IMergeStrategy
    {
        private readonly MergeNextIterationMergeStrategy mergeNextIteration;
        private readonly IRepositoryMediator repository;
        private readonly IGitCli cli;

        public ForceFreshMergeStrategy(MergeNextIterationMergeStrategy mergeNextIteration, IRepositoryMediator repository, IGitCli cli)
        {
            this.mergeNextIteration = mergeNextIteration;
            this.repository = repository;
            this.cli = cli;
        }

        public async Task AfterCreate(BranchGroup group, string latestBranchName, string createdBranchName, Func<IReactiveProcess, RepositoryActionReactiveProcessEntry> appendProcess)
        {
            var allRefs = await repository.GetAllBranchRefs().FirstOrDefaultAsync();
            var targetUpdateBranchName = createdBranchName == group.GroupName
                ? await repository.GetNextCandidateBranch(group).FirstOrDefaultAsync()
                : group.GroupName;
            await appendProcess(cli.Push(createdBranchName, targetUpdateBranchName, force: true));
            var createdRef = await repository.GetBranchRef(createdBranchName).FirstOrDefaultAsync();
            await repository.BranchUpdated(targetUpdateBranchName, createdRef, await repository.GetBranchRef(targetUpdateBranchName).FirstOrDefaultAsync());
        }

        public Task<ImmutableList<NeededMerge>> FindNeededMerges(string latestBranchName, ImmutableList<NeededMerge> upstreamBranches)
        {
            return mergeNextIteration.FindNeededMerges(latestBranchName, upstreamBranches);
        }

        public Task<bool> NeedsCreate(string latestBranchName, ImmutableList<NeededMerge> upstreamBranches)
        {
            return mergeNextIteration.NeedsCreate(latestBranchName, upstreamBranches);
        }
    }
}