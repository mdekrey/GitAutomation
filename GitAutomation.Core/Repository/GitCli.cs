using GitAutomation.Processes;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitAutomation.Repository
{
    // TODO - extract public interface for unit tests
    class GitCli
    {
        private static readonly Regex remoteBranches = new Regex(@"^(?<commit>\S+)\s+refs/heads/(?<branch>.+)");

        private readonly IReactiveProcessFactory reactiveProcessFactory;
        private readonly string checkoutPath;
        private readonly string repository;

        public GitCli(IReactiveProcessFactory factory, IOptions<GitRepositoryOptions> options)
        {
            this.reactiveProcessFactory = factory;
            this.checkoutPath = options.Value.CheckoutPath;
            this.repository = options.Value.Repository;

        }

        public bool IsGitInitialized =>
            Directory.Exists(Path.Combine(checkoutPath, ".git"));

        private IReactiveProcess RunGit(params string[] args)
        {
            return RunGit(args, null);
        }
        
        private IReactiveProcess RunGit(IEnumerable<string> args, Action<System.Diagnostics.ProcessStartInfo> setup)
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo(
                "git",
                string.Join(
                    " ",
                    args.Select(arg =>
                        arg.Contains("\"") || arg.Contains(" ")
                            ? $"\"{arg.Replace(@"\", @"\\").Replace("\"", "\\\"")}\""
                            : arg
                    )
                )
            )
            {
                WorkingDirectory = checkoutPath,
            };
            setup?.Invoke(startInfo);
            return reactiveProcessFactory.BuildProcess(startInfo);
        }

        public IReactiveProcess Clone()
        {
            Directory.Exists(checkoutPath);
            return RunGit("clone", repository, checkoutPath);
        }

        public IReactiveProcess Config(string configKey, string configValue)
        {
            return RunGit("config", configKey, configValue);
        }

        public IReactiveProcess Fetch()
        {
            return RunGit("fetch", "--prune");
        }

        public IReactiveProcess GetRemoteBranches()
        {
            return RunGit("ls-remote", "--heads", "origin");
        }

        public IReactiveProcess Reset()
        {
            return RunGit("reset", "--hard");
        }

        public IReactiveProcess Clean()
        {
            return RunGit("clean", "-fx");
        }

        /// <summary>
        /// Yields commitish of common ancestor
        /// </summary>
        public IReactiveProcess MergeBase(string branchA, string branchB)
        {
            return RunGit("merge-base", RemoteBranch(branchA), RemoteBranch(branchB));
        }


        public IReactiveProcess MergeBaseCommits(string branchA, string branchB)
        {
            return RunGit("merge-base", branchA, branchB);
        }

        public IReactiveProcess AnnotatedTag(string tagName, string message)
        {
            return RunGit("tag", "-a", tagName, "-m", message);
        }

        /// <summary>
        /// Yields commitish of branch
        /// </summary>
        public IReactiveProcess ShowRef(string branchName)
        {
            return RunGit("show-ref", RemoteBranch(branchName), "--hash");
        }

        public IReactiveProcess DeleteRemote(string branchName)
        {
            return RunGit("push", "origin", "--delete", branchName);
        }

        public IReactiveProcess CheckoutRemote(string branchName)
        {
            return RunGit("checkout", "-B", branchName, "--track", RemoteBranch(branchName));
        }

        public IReactiveProcess CheckoutNew(string branchName)
        {
            return RunGit("checkout", "-B", branchName);
        }

        public IReactiveProcess MergeRemote(string branchName, string message = null, string commitDate = null)
        {
            var parameters = new List<string>
            {
                "merge", RemoteBranch(branchName)
            };
            if (message != null)
            {
                parameters.AddRange(new[]
                {
                    "-m", message
                });
            }
            return RunGit(parameters, startInfo =>
            {
                if (commitDate != null)
                {
                    startInfo.EnvironmentVariables.Add("GIT_COMMITTER_DATE", commitDate);
                    startInfo.EnvironmentVariables.Add("GIT_AUTHOR_DATE", commitDate);
                }
            });
        }

        public IReactiveProcess MergeFastForward(string branchName)
        {
            return RunGit("merge", RemoteBranch(branchName), "--ff-only");
        }

        public IReactiveProcess Push(string branchName, string remoteBranchName = null)
        {
            if (remoteBranchName == null)
            {
                return RunGit("push", "origin", branchName);
            }
            else
            {
                return RunGit("push", "origin", $"{branchName}:{remoteBranchName}");
            }
        }

        public IReactiveProcess GetCommitTimestamps(params string[] commitishes)
        {
            return RunGit(new string[] { "show", "--format=%cI", "-s" }.Concat(commitishes).ToArray());
        }

        public IReactiveProcess GetCommitParents(string commitish)
        {
            return RunGit("show", "--format=%P", "-s", commitish);
        }

        public static IObservable<ImmutableList<GitRef>> BranchListingToRefs(IObservable<OutputMessage> refListing)
        {

            return (
                from output in refListing
                where output.Channel == OutputChannel.Out
                let remoteBranchLine = output.Message
                let remoteBranch = remoteBranches.Match(remoteBranchLine)
                where remoteBranch.Success
                select new GitRef { Commit = remoteBranch.Groups["commit"].Value, Name = remoteBranch.Groups["branch"].Value }
            )
                .Aggregate(ImmutableList<GitRef>.Empty, (list, next) => list.Add(next));
        }

        public string RemoteBranch(string branchName) => $"origin/{branchName}";

    }
}
