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
using GitAutomation.Repository.Actions;

namespace GitAutomation.Repository
{
    class RepositoryState : IRepositoryState
    {
        enum QueueAlterationKind
        {
            Remove,
            Append,
        }
        struct QueueAlteration
        {
            public QueueAlterationKind Kind;
            public IRepositoryAction Target;
        }

        private readonly IObservable<ImmutableList<IRepositoryAction>> repositoryActions;
        private readonly IObservable<OutputMessage> repositoryActionProcessor;
        private readonly IObservable<ImmutableList<OutputMessage>> repositoryActionProcessorLog;
        private readonly IObserver<QueueAlteration> queueAlterations;

        private readonly string checkoutPath;

        private readonly IObservable<Unit> allUpdates;
        private readonly IObservable<ImmutableList<GitCli.GitRef>> remoteBranches;

        public event EventHandler Updated;

        public RepositoryState(IOptions<GitRepositoryOptions> options, IServiceProvider serviceProvider)
        {
            this.checkoutPath = options.Value.CheckoutPath;

            var queueAlterations = new Subject<QueueAlteration>();
            this.queueAlterations = queueAlterations;

            var repositoryActions = queueAlterations.Scan(ImmutableList<IRepositoryAction>.Empty, (list, alteration) =>
            {
                if (alteration.Kind == QueueAlterationKind.Append)
                {
                    return list.Add(alteration.Target);
                }
                else if (alteration.Kind == QueueAlterationKind.Remove)
                {
                    return list.Remove(alteration.Target);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }).Replay(1);
            repositoryActions.Connect();
            this.repositoryActions = repositoryActions;

            this.repositoryActionProcessor = repositoryActions.Select(action => action.FirstOrDefault()).DistinctUntilChanged()
                .Select(action => action == null
                    ? Observable.Empty<OutputMessage>()
                    : action.PerformAction(serviceProvider).Catch<OutputMessage, Exception>(ex =>
                    {
                        // TODO - better logging
                        return Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"Action {action.ActionType} encountered an exception: {ex.Message}" });
                    }).Finally(() =>
                    {
                        this.queueAlterations.OnNext(new QueueAlteration { Kind = QueueAlterationKind.Remove, Target = action });
                    }))
                .Switch().Publish().RefCount();

            this.repositoryActionProcessorLog = repositoryActionProcessor
                .Scan(
                    ImmutableList<OutputMessage>.Empty,
                    (list, next) =>
                        (
                            list.Count >= 100
                                ? list.RemoveRange(0, list.Count - 99)
                                : list
                        ).Add(next)
                ).Replay(1).ConnectFirst();

            this.allUpdates = Observable.FromEventPattern<EventHandler, EventArgs>(
                handler => this.Updated += handler,
                handler => this.Updated -= handler
            ).Select(_ => Unit.Default);
            this.remoteBranches = BuildRemoteBranches();
        }

        private IObservable<OutputMessage> EnqueueAction(IRepositoryAction resetAction)
        {
            this.queueAlterations.OnNext(new QueueAlteration { Kind = QueueAlterationKind.Append, Target = resetAction });
            return resetAction.DeferredOutput;
        }

        #region Reset

        public IObservable<OutputMessage> Reset()
        {
            return EnqueueAction(new ClearAction());
        }
        
        #endregion

        #region Updates

        protected virtual void OnUpdated()
        {
            Updated?.Invoke(this, EventArgs.Empty);
        }

        public IObservable<OutputMessage> CheckForUpdates()
        {
            return EnqueueAction(new UpdateAction()).Finally(() => this.OnUpdated());
        }

        #endregion

        private IObservable<ImmutableList<GitCli.GitRef>> BuildRemoteBranches()
        {
            return Observable.Merge(
                allUpdates
                    .StartWith(Unit.Default)
                    .Select(_ =>
                    {
                        EnqueueAction(new EnsureInitializedAction());
                        return EnqueueAction(new GetRemoteBranchesAction());
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

        public IObservable<OutputMessage> CheckDownstreamMerges(string[] upstreamBranches, string downstreamBranch)
        {
            return EnqueueAction(new MergeDownstreamAction(upstreamBranches: upstreamBranches, downstreamBranch: downstreamBranch));
        }

        public IObservable<OutputMessage> ProcessActions()
        {
            return this.repositoryActionProcessor;
        }
        public IObservable<ImmutableList<OutputMessage>> ProcessActionsLog => this.repositoryActionProcessorLog;
        public IObservable<ImmutableList<IRepositoryAction>> ActionQueue => this.repositoryActions;
    }
}
