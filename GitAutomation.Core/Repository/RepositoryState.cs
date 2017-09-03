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
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;

namespace GitAutomation.Repository
{
    class RepositoryState : IRepositoryState
    {
        private readonly string checkoutPath;

        private readonly IObservable<Unit> allUpdates;
        private readonly IObservable<ImmutableList<GitCli.GitRef>> remoteBranches;
        private readonly IRepositoryOrchestration orchestration;
        private readonly GitCli cli;

        public event EventHandler Updated;

        public RepositoryState(IRepositoryOrchestration orchestration, IOptions<GitRepositoryOptions> options, GitCli cli)
        {
            this.checkoutPath = options.Value.CheckoutPath;
            this.orchestration = orchestration;
            this.cli = cli;

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

        public async Task<ImmutableList<string>> DetectUpstream(string branchName)
        {
            var remotes = await RemoteBranches().FirstAsync();
            Func<IReactiveProcess, Task<string>> getFirstOutput = target => 
                (from o in target.Output
                 where o.Channel == OutputChannel.Out
                 select o.Message).FirstOrDefaultAsync().ToTask();

            var allBranches = (from remote in remotes
                              select new
                              {
                                  branchName = remote,
                                  mergeBase = getFirstOutput(cli.MergeBase(remote, branchName)),
                                  commitish = getFirstOutput(cli.ShowRef(remote)),
                              }).ToArray();
            var currentCommitish = await getFirstOutput(cli.ShowRef(branchName));

            await Task.WhenAll(from branch in allBranches
                               from task in new[] { branch.mergeBase, branch.commitish }
                               select task);

            return (from branch in allBranches
                    where branch.commitish.Result == branch.mergeBase.Result
                    where branch.commitish.Result != currentCommitish
                    select branch.branchName).ToImmutableList();
        }


    }
}
