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
    class ConsolidateMergedAction : IRepositoryAction
    {
        private readonly Subject<OutputMessage> output = new Subject<OutputMessage>();
        private readonly ImmutableHashSet<string> originalBranches;
        private readonly string newBaseBranch;

        public string ActionType => "ConsolidateMerged";

        public ConsolidateMergedAction(IEnumerable<string> originalBranches, string newBaseBranch)
        {
            this.originalBranches = originalBranches.ToImmutableHashSet();
            this.newBaseBranch = newBaseBranch;
        }

        public JToken Parameters => JToken.FromObject(new
        {
            originalBranches = originalBranches.ToArray(),
            newBaseBranch
        });

        public IObservable<OutputMessage> DeferredOutput => output;

        public IObservable<OutputMessage> PerformAction(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<ConsolidateMergedActionProcess>(serviceProvider, originalBranches, newBaseBranch).Process().Multicast(output).RefCount();
        }

        private IObservable<OutputMessage> Queueable(IReactiveProcess reactiveProcess) => reactiveProcess.Output.Replay().ConnectFirst();

        private class ConsolidateMergedActionProcess : ComplexAction
        {
            private readonly GitCli cli;
            private readonly IRepositoryMediator repository;
            private readonly IUnitOfWorkFactory workFactory;
            private readonly ImmutableHashSet<string> originalBranches;
            private readonly string newBaseBranch;

            public ConsolidateMergedActionProcess(GitCli cli, IRepositoryMediator repository, IUnitOfWorkFactory workFactory, IEnumerable<string> originalBranches, string newBaseBranch)
            {
                this.cli = cli;
                this.repository = repository;
                this.workFactory = workFactory;
                this.originalBranches = originalBranches.ToImmutableHashSet();
                this.newBaseBranch = newBaseBranch;
            }

            protected override async Task RunProcess()
            {
                var allBranches = await repository.AllBranches().FirstOrDefaultAsync();
                var targetBranch = allBranches.Find(branch => branch.BranchName == newBaseBranch);

                // 1. make sure everything is already merged
                // 2. run consolidate branches SQL
                // 3. delete old branches

                var consolidatingBranches = (await (from branch in allBranches.ToObservable()
                                                    where this.originalBranches.Contains(branch.BranchName) || branch.BranchNames.Any(this.originalBranches.Contains)
                                                    from result in GetLatestBranchTuple(branch)
                                                    select result
                                        ).ToArray()).ToImmutableHashSet();


                var branchesToRemove = await FindReadyToConsolidate(consolidatingBranches);

                // Integration branches remain separate. Even if they have one
                // parent, multiple things could depend on them and you don't
                // want to flatten out those commits too early.
                using (var unitOfWork = workFactory.CreateUnitOfWork())
                {
                    repository.ConsolidateBranches(branchesToRemove.Select(b => b.BranchName), targetBranch.BranchName, unitOfWork);

                    await unitOfWork.CommitAsync();
                }

                await (from branch in branchesToRemove.ToObservable()
                      from actualBranch in branch.BranchNames
                      from entry in AppendProcess(Queueable(cli.DeleteRemote(actualBranch)))
                      select entry).StartWith(new OutputMessage());
            }

            private IObservable<(BranchBasicDetails branch, string latestBranchName)> GetLatestBranchTuple(BranchBasicDetails branch)
            {
                return from latestBranchName in repository.LatestBranchName(branch).FirstOrDefaultAsync()
                       select (branch, latestBranchName);
            }

            private async Task<ImmutableList<BranchBasicDetails>> FindReadyToConsolidate(IImmutableSet<(BranchBasicDetails branch, string latestBranchName)> originalBranches)
            {
                return await (from upstreamBranch in originalBranches.ToObservable()
                              from hasOutstandingCommit in cli.HasOutstandingCommits(upstreamBranch: upstreamBranch.latestBranchName, downstreamBranch: newBaseBranch)
                              where !hasOutstandingCommit
                              select upstreamBranch.branch)
                    .ToArray()
                    .Select(items => items.ToImmutableList());
            }

            private IObservable<OutputMessage> Queueable(IReactiveProcess reactiveProcess) => reactiveProcess.Output.Replay().ConnectFirst();
        }
    }
}
