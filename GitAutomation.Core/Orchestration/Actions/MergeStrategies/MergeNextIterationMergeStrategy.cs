using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using GitAutomation.BranchSettings;
using System.Reactive.Linq;
using GitAutomation.Repository;
using GitAutomation.Processes;
using System.Linq;
using GitAutomation.GitService;

namespace GitAutomation.Orchestration.Actions.MergeStrategies
{
    class MergeNextIterationMergeStrategy : IMergeStrategy
    {
        private readonly NormalMergeStrategy normalStrategy;
        private readonly IGitCli cli;
        private readonly IGitServiceApi gitServiceApi;

        public MergeNextIterationMergeStrategy(NormalMergeStrategy normalStrategy, IGitCli cli, IGitServiceApi gitServiceApi)
        {
            this.normalStrategy = normalStrategy;
            this.cli = cli;
            this.gitServiceApi = gitServiceApi;
        }

        public async Task<bool> NeedsCreate(string latestBranchName, ImmutableList<NeededMerge> upstreamBranches)
        {
            if (await normalStrategy.NeedsCreate(latestBranchName, upstreamBranches))
            {
                return true;
            }


            // It's okay if there are still other "upstreamBranches" that didn't get merged, because we already did that check
            var neededRefs = (await (from branchName in upstreamBranches.ToObservable()
                                     from branchRef in cli.ShowRef(branchName.BranchName).FirstOutputMessage()
                                     select branchRef).ToArray()).ToImmutableHashSet();

            var currentHead = await cli.ShowRef(latestBranchName).FirstOutputMessage();
            if (!neededRefs.Contains(currentHead))
            {
                // The latest commit of the branch isn't one that we needed. That means it's probably a merge commit!
                Func<string, IObservable<string[]>> getParents = (commitish) =>
                    cli.GetCommitParents(commitish).FirstOutputMessage().Select(commit => commit.Split(' '));
                var parents = await getParents(cli.RemoteBranch(latestBranchName));
                while (parents.Length > 1)
                {
                    // Figure out what the other commits are, if any
                    var other = parents.Where(p => !neededRefs.Contains(p)).ToArray();
                    if (other.Length != 1)
                    {
                        break;
                    }
                    else
                    {
                        // If there's another commit, it might be a merge of two of our other requireds. Check it!
                        parents = await getParents(other[0]);
                    }
                }
                // If this isn't a merge, then we had a non-merge commit in there; we don't care about the parent.
                // If it is a merge, we either have 2 "others" or no "others", so looking at one parent will tell us:
                // If it's a required commit, we're good. If it's not, we need to recreate the branch.
                return parents.Length < 2 || !neededRefs.Contains(parents[0]);
            }
            return false;
        }

        public Task<ImmutableList<NeededMerge>> FindNeededMerges(string latestBranchName, ImmutableList<NeededMerge> upstreamBranches)
        {
            return Task.FromResult(upstreamBranches);
        }

        public async Task AfterCreate(string latestBranchName, string branchName)
        {
            if (latestBranchName != null && latestBranchName != branchName)
            {
                await gitServiceApi.MigrateOrClosePullRequests(fromBranch: latestBranchName, toBranch: branchName);
            }
        }
    }
}
