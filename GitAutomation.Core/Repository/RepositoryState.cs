using GitAutomation.Processes;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Reactive.Subjects;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace GitAutomation.Repository
{
    class RepositoryState : IRepositoryState
    {
        private readonly string checkoutPath;
        private readonly IConnectableObservable<OutputMessage> initialize;
        private System.Reactive.Disposables.CompositeDisposable initializeConnections = new System.Reactive.Disposables.CompositeDisposable();
        private readonly GitCli cli;

        public RepositoryState(GitCli cli, IOptions<GitRepositoryOptions> options)
        {
            this.cli = cli;
            this.checkoutPath = options.Value.CheckoutPath;

            this.initialize = BuildInitialization();
        }

        #region Initialize and Reset

        public IObservable<string> Reset()
        {
            var temp = this.initializeConnections;
            initializeConnections = new System.Reactive.Disposables.CompositeDisposable();
            temp.Dispose();

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
                return proc.Output.Subscribe(observer);
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


        public IObservable<string[]> RemoteBranches()
        {
            return InitializeThenSwitch(
                onSuccess: from gitref in cli.GetRemoteBranches()
                           select gitref.Name
            )
            .Aggregate(ImmutableList<string>.Empty, (list, next) => list.Add(next))
            .Select(list => list.ToArray());
        }
    }
}
