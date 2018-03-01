using GitAutomation.BranchSettings;
using GitAutomation.Processes;
using GitAutomation.Repository;
using GitAutomation.Work;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace GitAutomation.Orchestration.Actions
{
    class ConsolidateMergedAction : ComplexAction<ConsolidateMergedAction.ConsolidateMergedActionProcess>
    {
        private readonly string sourceBranch;
        private readonly string newBaseBranch;

        public override string ActionType => "ConsolidateMerged";

        public ConsolidateMergedAction(string sourceBranch, string newBaseBranch)
        {
            this.sourceBranch = sourceBranch;
            this.newBaseBranch = newBaseBranch;
        }

        public override JToken Parameters => JToken.FromObject(new
        {
            sourceBranch = sourceBranch,
            newBaseBranch
        });

        public string SourceBranch => sourceBranch;
        public string NewBaseBranch => newBaseBranch;

        internal override object[] GetExtraParameters()
        {
            return new object[] {
                sourceBranch,
                newBaseBranch
            };
        }
        
        public class ConsolidateMergedActionProcess : ComplexActionInternal
        {
            private readonly IGitCli cli;
            private readonly IRepositoryMediator repository;
            private readonly IBranchSettings branchSettings;
            private readonly IUnitOfWorkFactory workFactory;
            private readonly string sourceBranch;
            private readonly string newBaseBranch;

            public ConsolidateMergedActionProcess(IGitCli cli, IRepositoryMediator repository, IBranchSettings branchSettings, IUnitOfWorkFactory workFactory, string sourceBranch, string newBaseBranch)
            {
                this.cli = cli;
                this.repository = repository;
                this.branchSettings = branchSettings;
                this.workFactory = workFactory;
                this.sourceBranch = sourceBranch;
                this.newBaseBranch = newBaseBranch;
            }

            protected override async Task RunProcess()
            {
                var targetBranch = await repository.GetBranchDetails(newBaseBranch).FirstOrDefaultAsync();

                // 1. make sure everything is already merged
                // 2. run consolidate branches SQL
                // 3. delete old branches
                ImmutableList<BranchGroupCompleteData> branchesToRemove = await GetBranchesToRemove(targetBranch);

                // Integration branches remain separate. Even if they have one
                // parent, multiple things could depend on them and you don't
                // want to flatten out those commits too early.
                using (var unitOfWork = workFactory.CreateUnitOfWork())
                {
                    repository.ConsolidateBranches(branchesToRemove.Select(b => b.GroupName), targetBranch.GroupName, unitOfWork);

                    await unitOfWork.CommitAsync();
                }

                // TODO - branches could get deleted here that were ignored in the `ConsolidateBranches` logic.
                await (from branch in branchesToRemove.ToObservable()
                       from actualBranch in branch.Branches
                       from entry in AppendProcess(cli.DeleteRemote(actualBranch.Name)).WaitUntilComplete().ContinueWith<int>((t) => 0)
                       select entry);

                repository.CheckForUpdates();
            }

            private async Task<ImmutableList<BranchGroupCompleteData>> GetBranchesToRemove(BranchGroupCompleteData targetBranch)
            {
                if (targetBranch.DirectDownstreamBranchGroups.Contains(newBaseBranch))
                {
                    // We're deleting an old branch, and all of its stuff moves down. This is like an integration branch rolling up. Easy.
                    return new[] { targetBranch }.ToImmutableList();
                }
                var allBranches = await repository.AllBranches().FirstOrDefaultAsync();

                var actualBranches = await repository.DetectUpstream(sourceBranch);
                var upstreamBranches = await branchSettings.GetAllUpstreamBranches(sourceBranch);

                // Filter to only the actual upstream branches; don't just remove anything that is downstream and happens to match!
                actualBranches = actualBranches.Intersect(upstreamBranches.Select(b => b.GroupName)).ToImmutableList();

                // FIXME - this is the _branches_ not the _groups_ that are up-to-date. That should be okay for these purposes.
                var downstream = (await branchSettings.GetBranchDetails(newBaseBranch).FirstAsync()).DownstreamBranchGroups;

                var consolidatingBranches = (await (from branch in allBranches.ToObservable()
                                                    where actualBranches.Contains(branch.GroupName) || branch.Branches.Select(b => b.Name).Any(actualBranches.Contains)
                                                    where downstream.Contains(branch.GroupName)
                                                    from result in GetLatestBranchTuple(branch)
                                                    select result
                                        ).ToArray()).ToImmutableHashSet();


                var branchesToRemove = await FindReadyToConsolidate(consolidatingBranches);
                branchesToRemove = branchesToRemove.Concat(allBranches.Where(g => g.Branches.Select(b => b.Name).Contains(sourceBranch))).ToImmutableList();
                return branchesToRemove;
            }

            private IObservable<(BranchGroupCompleteData branch, string latestBranchName)> GetLatestBranchTuple(BranchGroupCompleteData branch)
            {
                return from latestBranchName in repository.LatestBranchName(branch).FirstOrDefaultAsync()
                       select (branch, latestBranchName);
            }

            private async Task<ImmutableList<BranchGroupCompleteData>> FindReadyToConsolidate(IImmutableSet<(BranchGroupCompleteData branch, string latestBranchName)> originalBranches)
            {
                return await (from upstreamBranch in originalBranches.ToObservable()
                              from hasOutstandingCommit in cli.HasOutstandingCommits(upstreamBranch: upstreamBranch.latestBranchName, downstreamBranch: newBaseBranch)
                              where !hasOutstandingCommit
                              select upstreamBranch.branch)
                    .ToArray()
                    .Select(items => items.ToImmutableList());
            }
        }
    }
}
