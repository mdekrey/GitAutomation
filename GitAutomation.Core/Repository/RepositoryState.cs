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
                    : action.PerformAction(serviceProvider).Finally(() =>
                    {
                        this.queueAlterations.OnNext(new QueueAlteration { Kind = QueueAlterationKind.Remove, Target = action });
                    }))
                .Switch().Publish().RefCount();
            
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

        #region Initialize and Reset

        public IObservable<OutputMessage> Reset()
        {
            return EnqueueAction(new ResetAction());
        }

        //public IObservable<OutputMessage> Initialize()
        //{
        //    return Observable.Create<OutputMessage>(observer =>
        //    {
        //        this.initializeConnections.Add(initialize.Connect());
        //        return initialize.Subscribe(observer);
        //    });
        //}

        //private IObservable<bool> SuccessfulInitialize()
        //{
        //    return from message in Initialize()
        //           where message.Channel == OutputChannel.ExitCode
        //           select message.ExitCode == 0;
        //}

        //private IObservable<T> InitializeThen<T>(Func<T> onSuccess, Func<T> onFailure)
        //{
        //    return from isSuccess in SuccessfulInitialize()
        //           select isSuccess
        //                ? onSuccess()
        //                : onFailure();
        //}

        //private IObservable<T> InitializeThen<T>()
        //{
        //    return InitializeThen(() => Observable.Empty<T>(), () => Observable.Throw<T>(new Exception("Failed to initialize"))).Switch();
        //}

        //private IObservable<T> InitializeThenSwitch<T>(IObservable<T> onSuccess, IObservable<T> onFailure = null)
        //{
        //    return InitializeThen(
        //        onSuccess: () => onSuccess, 
        //        onFailure: onFailure != null 
        //            ? (Func<IObservable<T>>)(() => onFailure) 
        //            : Observable.Empty<T>
        //    ).Switch();
        //}

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
                .Publish().ConnectFirst();
        }

        public IObservable<string[]> RemoteBranches()
        {
            return remoteBranches
                .Select(list => list.Select(branch => branch.Name).ToArray());
        }

        public IObservable<OutputMessage> ProcessActions()
        {
            return this.repositoryActionProcessor;
        }
    }
}
