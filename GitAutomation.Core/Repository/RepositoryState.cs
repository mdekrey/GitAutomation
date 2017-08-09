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
        private static readonly Regex remoteBranches = new Regex(@"^(?<commit>\S+)\s+refs/heads/(?<branch>.+)");
        private struct Ref
        {
            public string Commit;
            public string Name;
        }

        private readonly string checkoutPath;
        private readonly IReactiveProcessFactory reactiveProcessFactory;
        private readonly string repository;
        private readonly IConnectableObservable<OutputMessage> initialize;
        private System.Reactive.Disposables.CompositeDisposable initializeConnections = new System.Reactive.Disposables.CompositeDisposable();

        public RepositoryState(IReactiveProcessFactory factory, IOptions<GitRepositoryOptions> options)
        {
            this.reactiveProcessFactory = factory;
            this.repository = options.Value.Repository;
            this.checkoutPath = options.Value.CheckoutPath;

            this.initialize = Observable.Create<OutputMessage>(observer =>
            {
                var info = Directory.CreateDirectory(checkoutPath);
                var children = info.GetFileSystemInfos();
                var proc = RunGit("clone", repository, checkoutPath);
                return proc.Output.Subscribe(observer);
            }).Publish().RefCount().Replay(1);
        }

        private IReactiveProcess RunGit(params string[] args)
        {
            return reactiveProcessFactory.BuildProcess(new System.Diagnostics.ProcessStartInfo(
                "git",
                string.Join(" ", args.Select(arg => arg.Contains("\"") ? $"\"{arg.Replace(@"\", @"\\").Replace("\"", "\\\"")}\"" : arg))
            )
            {
                WorkingDirectory = checkoutPath
            });
        }

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


        public IObservable<string[]> RemoteBranches()
        {
            return (from remoteBranchLine in SuccessfulInitialize()
                                            .Select(isSuccess => isSuccess
                                                ? from output in RunGit("ls-remote", "--heads", "origin").Output
                                                  where output.Channel == OutputChannel.Out
                                                  select output.Message
                                                : Observable.Empty<string>()
                                            )
                                            .Switch()
                   select remoteBranches.Match(remoteBranchLine) into remoteBranch
                   where remoteBranch.Success
                   select new Ref { Commit = remoteBranch.Groups["commit"].Value, Name = remoteBranch.Groups["branch"].Value } into gitref
                   select gitref.Name)
            .Aggregate(ImmutableList<string>.Empty, (list, next) => list.Add(next))
            .Select(list => list.ToArray());
        }
    }
}
