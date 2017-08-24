using GitAutomation.Processes;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive;
using System.Collections.Immutable;
using GitAutomation.Orchestration.Actions;
using GitAutomation.BranchSettings;
using GitAutomation.Orchestration;

namespace GitAutomation.Repository
{
    class RepositoryState : IRepositoryState
    {
        private readonly string checkoutPath;

        private readonly IObservable<Unit> allUpdates;
        private readonly IObservable<ImmutableList<GitCli.GitRef>> remoteBranches;
        private readonly IBranchSettings branchSettings;
        private readonly IRepositoryOrchestration orchestration;

        public event EventHandler Updated;

        public RepositoryState(IRepositoryOrchestration orchestration, IBranchSettings branchSettings, IOptions<GitRepositoryOptions> options, IServiceProvider serviceProvider)
        {
            this.checkoutPath = options.Value.CheckoutPath;
            this.branchSettings = branchSettings;
            this.orchestration = orchestration;

            this.allUpdates = Observable.FromEventPattern<EventHandler, EventArgs>(
                handler => this.Updated += handler,
                handler => this.Updated -= handler
            ).Select(_ => Unit.Default);
            this.remoteBranches = BuildRemoteBranches();
        }

        #region Reset

        public IObservable<OutputMessage> DeleteRepository()
        {
            return orchestration.EnqueueAction(new ClearAction());
        }
        
        #endregion

        #region Updates

        protected virtual void OnUpdated()
        {
            Updated?.Invoke(this, EventArgs.Empty);
        }

        public IObservable<OutputMessage> CheckForUpdates()
        {
            return orchestration.EnqueueAction(new UpdateAction()).Finally(() => this.OnUpdated());
        }

        #endregion

        private IObservable<ImmutableList<GitCli.GitRef>> BuildRemoteBranches()
        {
            return Observable.Merge(
                allUpdates
                    .StartWith(Unit.Default)
                    .Select(_ =>
                    {
                        orchestration.EnqueueAction(new EnsureInitializedAction());
                        return orchestration.EnqueueAction(new GetRemoteBranchesAction());
                    })
                    .Select(GitCli.BranchListingToRefs)
            )
                .Replay(1).ConnectFirst();
        }

        public IObservable<string[]> RemoteBranches()
        {
            return remoteBranches
                .Select(list => list.Select(branch => branch.Name).ToArray());
        }

        public IObservable<OutputMessage> DeleteBranch(string branchName)
        {
            return orchestration.EnqueueAction(new DeleteBranchAction(branchName));
        }

        public IObservable<OutputMessage> CheckDownstreamMerges(string downstreamBranch)
        {
            return orchestration.EnqueueAction(new MergeDownstreamAction(downstreamBranch: downstreamBranch));
        }

        public IObservable<OutputMessage> CheckAllDownstreamMerges()
        {
            return RemoteBranches().Take(1).SelectMany(allBranches => allBranches.ToObservable())
                        .SelectMany(
                            upstream => 
                                branchSettings
                                    .GetDownstreamBranches(upstream)
                                    .Take(1)
                                    .SelectMany(branches => 
                                        branches
                                            .ToObservable()
                                            .Select(downstream => new { upstream, downstream })
                                    )
                        )
                        .ToList()
                        .SelectMany(all => all.Select(each => each.downstream).Distinct().ToObservable())
                        .SelectMany(upstreamBranch => CheckDownstreamMerges(upstreamBranch));
        }

        public IObservable<OutputMessage> ConsolidateServiceLine(string releaseCandidateBranch, string serviceLineBranch, string tagName)
        {
            return orchestration.EnqueueAction(new ConsolidateServiceLineAction(releaseCandidateBranch, serviceLineBranch, tagName));
        }


    }
}
