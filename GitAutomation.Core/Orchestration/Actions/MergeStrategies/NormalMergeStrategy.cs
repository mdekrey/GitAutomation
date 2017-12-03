using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using GitAutomation.BranchSettings;
using System.Reactive.Linq;
using GitAutomation.Processes;

namespace GitAutomation.Orchestration.Actions.MergeStrategies
{
    class NormalMergeStrategy : IMergeStrategy
    {
        private readonly IRepositoryMediator repository;

        public NormalMergeStrategy(IRepositoryMediator repository)
        {
            this.repository = repository;
        }

        public async Task<bool> NeedsCreate(string latestBranchName, ImmutableList<NeededMerge> upstreamBranches)
        {
            return await repository.GetBranchRef(latestBranchName).Take(1) == null;
        }


        public async Task<ImmutableList<NeededMerge>> FindNeededMerges(string latestBranchName, ImmutableList<NeededMerge> allUpstreamBranches)
        {
            return await (from upstreamBranch in FilterMergableBranches(allUpstreamBranches.ToObservable())
                          from hasOutstandingCommit in repository.HasOutstandingCommits(upstreamBranch: upstreamBranch.BranchName, downstreamBranch: latestBranchName)
                          where hasOutstandingCommit
                          select upstreamBranch)
                .ToArray()
                .Select(items => items.ToImmutableList());
        }
        
        protected IObservable<NeededMerge> FilterMergableBranches(IObservable<NeededMerge> branches)
        {
            return from upstreamBranch in branches
                   where upstreamBranch.BranchName != null
                   from isBad in repository.IsBadBranch(upstreamBranch.BranchName)
                   where !isBad
                   select upstreamBranch;
        }

        public Task AfterCreate(BranchGroup group, string latestBranchName, string branchName, Func<IReactiveProcess, RepositoryActionReactiveProcessEntry> appendProcess)
        {
            return Task.CompletedTask;
        }
    }
}
