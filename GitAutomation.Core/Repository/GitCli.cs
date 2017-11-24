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
using System.Threading.Tasks;

namespace GitAutomation.Repository
{
    class _GitCli : IGitCli
    {

        private readonly IReactiveProcessFactory reactiveProcessFactory;
        private readonly string checkoutPath;
        private readonly string repository;
        private readonly string userName;
        private readonly string userEmail;

        public _GitCli(IReactiveProcessFactory factory, string checkoutPath, string repository, string userName, string userEmail)
        {
            this.reactiveProcessFactory = factory;
            this.checkoutPath = checkoutPath;
            this.repository = repository;
            this.userName = userName;
            this.userEmail = userEmail;
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

        public IReactiveProcess RemoveRemoteTrackingBranch(string branchName)
        {
            return RunGit("branch", "-rd", RemoteBranch(branchName));
        }

        public IReactiveProcess Checkout(string branchName)
        {
            return RunGit("checkout", branchName);
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

        public string RemoteBranch(string branchName) => $"origin/{branchName}";

        public async Task Initialize()
        {
            if (!Directory.Exists(checkoutPath))
            {
                Directory.CreateDirectory(checkoutPath);
            }

            await Clone().ActiveState;
            await Config("user.name", userName).ActiveState;
            await Config("user.email", userEmail).ActiveState;
        }
    }
}
