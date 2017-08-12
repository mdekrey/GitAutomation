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

namespace GitAutomation.Repository
{
    class RepositoryState : IRepositoryState
    {
        private readonly string checkoutPath;
        private readonly IConnectableObservable<OutputMessage> initialize;
        private CompositeDisposable initializeConnections = new CompositeDisposable();
        private bool isInitialized = false;
        private readonly GitCli cli;
        private readonly IObservable<OutputMessage> update;
        private readonly IObservable<Unit> allUpdates;
        private readonly IObservable<ImmutableList<GitCli.GitRef>> remoteBranches;

        public event EventHandler Updated;

        public RepositoryState(GitCli cli, IOptions<GitRepositoryOptions> options)
        {
            this.cli = cli;
            this.checkoutPath = options.Value.CheckoutPath;

            this.initialize = BuildInitialization();
            this.update = BuildUpdater();
            this.allUpdates = Observable.FromEventPattern<EventHandler, EventArgs>(
                handler => this.Updated += handler,
                handler => this.Updated -= handler
            ).Select(_ => Unit.Default);
            this.remoteBranches = BuildRemoteBranches();
        }

        #region Initialize and Reset

        public IObservable<string> Reset()
        {
            var temp = this.initializeConnections;
            initializeConnections = new CompositeDisposable();
            temp.Dispose();
            isInitialized = false;

            if (Directory.Exists(checkoutPath))
            {
                // starting from an old system? maybe... but I don't want to handle that yet.
                Directory.Delete(checkoutPath, true);
            }
            return Observable.Empty<string>();
        }

        private IConnectableObservable<OutputMessage> BuildInitialization()
        {
            return Observable.Create<OutputMessage>(observer =>
            {
                var info = Directory.CreateDirectory(checkoutPath);
                var proc = cli.Clone();
                return proc.Output.Do(_ =>
                {
                    if (_.Channel == OutputChannel.ExitCode)
                    {
                        isInitialized = true;
                        OnUpdated();
                    }
                }).Subscribe(observer);
            }).Publish().RefCount().Replay(1);
        }

        public IObservable<OutputMessage> Initialize()
        {
            return Observable.Create<OutputMessage>(observer =>
            {
                this.initializeConnections.Add(initialize.Connect());
                return initialize.Subscribe(observer);
            });
        }

        private IObservable<bool> SuccessfulInitialize()
        {
            return from message in Initialize()
                   where message.Channel == OutputChannel.ExitCode
                   select message.ExitCode == 0;
        }

        private IObservable<T> InitializeThen<T>(Func<T> onSuccess, Func<T> onFailure)
        {
            return from isSuccess in SuccessfulInitialize()
                   select isSuccess
                        ? onSuccess()
                        : onFailure();
        }

        private IObservable<T> InitializeThen<T>()
        {
            return InitializeThen(() => Observable.Empty<T>(), () => Observable.Throw<T>(new Exception("Failed to initialize"))).Switch();
        }

        private IObservable<T> InitializeThenSwitch<T>(IObservable<T> onSuccess, IObservable<T> onFailure = null)
        {
            return InitializeThen(
                onSuccess: () => onSuccess, 
                onFailure: onFailure != null 
                    ? (Func<IObservable<T>>)(() => onFailure) 
                    : Observable.Empty<T>
            ).Switch();
        }

        #endregion

        #region Updates

        protected virtual void OnUpdated()
        {
            Updated?.Invoke(this, EventArgs.Empty);
        }

        private IObservable<OutputMessage> BuildUpdater()
        {
            return Observable.Create<OutputMessage>(observer =>
            {
                var proc = cli.Fetch();
                return proc.Output.Do(message =>
                {
                    if (message.Channel == OutputChannel.ExitCode)
                    {
                        OnUpdated();
                    }
                }).Subscribe(observer);
            }).Publish().RefCount();
        }

        public void BeginCheckForUpdates()
        {
            if (!isInitialized)
            {
                Initialize().Subscribe();
            }
            else
            {
                update.TakeWhile(message => message.Channel != OutputChannel.ExitCode).Subscribe();
            }
        }

        #endregion

        private IObservable<ImmutableList<GitCli.GitRef>> BuildRemoteBranches()
        {
            return InitializeThen<ImmutableList<GitCli.GitRef>>()
                            .Concat(allUpdates.StartWith(Unit.Default).Select(_ => cli.GetRemoteBranches()).Switch())
                            .Publish().RefCount();
        }

        public IObservable<string[]> RemoteBranches()
        {
            return remoteBranches
                .Select(list => list.Select(branch => branch.Name).ToArray());
        }
    }
}
