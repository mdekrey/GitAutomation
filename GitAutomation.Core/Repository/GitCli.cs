using GitAutomation.Processes;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitAutomation.Repository
{
    class GitCli
    {
        private static readonly Regex remoteBranches = new Regex(@"^(?<commit>\S+)\s+refs/heads/(?<branch>.+)");
        public struct GitRef
        {
            public string Commit;
            public string Name;
        }

        private readonly IReactiveProcessFactory reactiveProcessFactory;
        private readonly string checkoutPath;
        private readonly string repository;

        public GitCli(IReactiveProcessFactory factory, IOptions<GitRepositoryOptions> options)
        {
            this.reactiveProcessFactory = factory;
            this.checkoutPath = options.Value.CheckoutPath;
            this.repository = options.Value.Repository;

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

        public IReactiveProcess Clone()
        {
            return RunGit("clone", repository, checkoutPath);
        }

        public IReactiveProcess Fetch()
        {
            return RunGit("fetch", "--prune");
        }

        public IObservable<ImmutableList<GitRef>> GetRemoteBranches()
        {
            return (
                from output in RunGit("ls-remote", "--heads", "origin").Output
                where output.Channel == OutputChannel.Out
                let remoteBranchLine = output.Message
                let remoteBranch = remoteBranches.Match(remoteBranchLine)
                where remoteBranch.Success
                select new GitRef { Commit = remoteBranch.Groups["commit"].Value, Name = remoteBranch.Groups["branch"].Value }
            )
                .Aggregate(ImmutableList<GitRef>.Empty, (list, next) => list.Add(next));
        }
    }
}
