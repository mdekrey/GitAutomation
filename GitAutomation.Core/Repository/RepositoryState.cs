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
using GitAutomation.Orchestration;

namespace GitAutomation.Repository
{
    class RepositoryState : IRepositoryState
    {
        private readonly string checkoutPath;

        private readonly IObservable<Unit> allUpdates;
        private readonly IObservable<ImmutableList<GitCli.GitRef>> remoteBranches;
        private readonly IRepositoryOrchestration orchestration;

        public event EventHandler Updated;

        public RepositoryState(IRepositoryOrchestration orchestration, IOptions<GitRepositoryOptions> options)
        {
            this.checkoutPath = options.Value.CheckoutPath;
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
            return orchestration.EnqueueAction(new UpdateAction()).Finally(OnUpdated);
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
                        // TODO - because listing remote branches doesn't affect the index, it doesn't need to be an action, but it does need to wait until initialization is ensured.
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
            return orchestration.EnqueueAction(new DeleteBranchAction(branchName)).Finally(OnUpdated);
        }


    }
}
