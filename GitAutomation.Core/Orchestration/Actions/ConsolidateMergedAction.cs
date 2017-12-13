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
        private readonly ImmutableHashSet<string> originalBranches;
        private readonly string newBaseBranch;

        public override string ActionType => "ConsolidateMerged";

        public ConsolidateMergedAction(IEnumerable<string> originalBranches, string newBaseBranch)
        {
            this.originalBranches = originalBranches.ToImmutableHashSet();
            this.newBaseBranch = newBaseBranch;
        }

        public override JToken Parameters => JToken.FromObject(new
        {
            originalBranches = originalBranches.ToArray(),
            newBaseBranch
        });

        public IEnumerable<string> OriginalBranches => originalBranches;
        public string NewBaseBranch => newBaseBranch;

        internal override object[] GetExtraParameters()
        {
            return new object[] {
                originalBranches,
                newBaseBranch
            };
        }
        
        public class ConsolidateMergedActionProcess : ComplexActionInternal
        {
            private readonly IGitCli cli;
            private readonly IRepositoryMediator repository;
            private readonly IUnitOfWorkFactory workFactory;
            private readonly ImmutableHashSet<string> originalBranches;
            private readonly string newBaseBranch;

            public ConsolidateMergedActionProcess(IGitCli cli, IRepositoryMediator repository, IUnitOfWorkFactory workFactory, IEnumerable<string> originalBranches, string newBaseBranch)
            {
                this.cli = cli;
                this.repository = repository;
                this.workFactory = workFactory;
                this.originalBranches = originalBranches.Except(new[] { newBaseBranch }).ToImmutableHashSet();
                this.newBaseBranch = newBaseBranch;
            }

            protected override async Task RunProcess()
            {
                var allBranches = await repository.AllBranches().FirstOrDefaultAsync();
                var targetBranch = allBranches.Find(branch => branch.GroupName == newBaseBranch);

                // 1. make sure everything is already merged
                // 2. run consolidate branches SQL
                // 3. delete old branches

                var consolidatingBranches = (await (from branch in allBranches.ToObservable()
                                                    where this.originalBranches.Contains(branch.GroupName) || branch.Branches.Select(b => b.Name).Any(this.originalBranches.Contains)
                                                    from result in GetLatestBranchTuple(branch)
                                                    select result
                                        ).ToArray()).ToImmutableHashSet();


                var branchesToRemove = await FindReadyToConsolidate(consolidatingBranches);

                // Integration branches remain separate. Even if they have one
                // parent, multiple things could depend on them and you don't
                // want to flatten out those commits too early.
                using (var unitOfWork = workFactory.CreateUnitOfWork())
                {
                    repository.ConsolidateBranches(branchesToRemove.Select(b => b.GroupName), targetBranch.GroupName, unitOfWork);

                    await unitOfWork.CommitAsync();
                }

                await (from branch in branchesToRemove.ToObservable()
                       from actualBranch in branch.Branches
                       from entry in AppendProcess(cli.DeleteRemote(actualBranch.Name)).WaitUntilComplete().ContinueWith<int>((t) => 0)
                       select entry);

                repository.CheckForUpdates();
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
