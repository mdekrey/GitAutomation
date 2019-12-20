using GitAutomation.DomainModels;
using GitAutomation.Web;
using LibGit2Sharp;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Branches
{
    public static class GitRepositoryUtilities
    {
        private static readonly MergeOptions standardMergeOptions = new MergeOptions
        {
            CommitOnSuccess = false
        };

        public static Repository CloneAsLocal(string checkoutPath, string workingPath, AutomationOptions automationOptions)
        {
            Repository.Init(workingPath, isBare: false);
            var repo = new Repository(workingPath);
            repo.Network.Remotes.Add(automationOptions.WorkingRemote, checkoutPath);
            Commands.Fetch(repo, automationOptions.WorkingRemote, new[] { "refs/heads/*:refs/heads/*" }, new FetchOptions { }, "");
            return repo;
        }


        public static void ResetAndClean(this Repository repo)
        {
            repo.Reset(ResetMode.Hard);
            foreach (var entry in repo.RetrieveStatus())
            {
                if (entry.State != FileStatus.Unaltered)
                {
                    System.IO.File.Delete(System.IO.Path.Combine(repo.Info.Path, entry.FilePath));
                }
            }
        }

        public static void EnsureRemote(this Repository repo, string remoteName, TargetRepositoryOptions options)
        {
            if (repo.Network.Remotes[remoteName]?.PushUrl != options.Remotes[remoteName].Url)
            {
                repo.Network.Remotes.Remove(remoteName);
            }
            if (repo.Network.Remotes[remoteName] == null)
            {
                repo.Network.Remotes.Add(remoteName, options.Remotes[remoteName].Url);
            }
        }

        public static void EnsureRemoteAndPush(this Repository repo, string remoteName, TargetRepositoryOptions options, params string[] refSpecs)
        {
            repo.EnsureRemote(remoteName, options);
            repo.Network.Push(repo.Network.Remotes[remoteName], refSpecs, new PushOptions
            {
                CredentialsProvider = options.Remotes[remoteName].ToCredentialsProvider()
            });
        }

        public static void EnsureRemoteAndPushBranch(this Repository repo, string fullBranchName, string fromCommitish, TargetRepositoryOptions options, IBranchNaming branchNaming)
        {
            var (baseRemote, outputBranchRemoteName) = branchNaming.SplitCheckoutRepositoryBranchName(fullBranchName);
            repo.EnsureRemoteAndPush(baseRemote, options, $"{fromCommitish}:refs/heads/{outputBranchRemoteName}");
        }

        public static MergeStatus CleanMerge(this Repository repo, Commit commit, Identity gitIdentity, string message)
        {
            var result = repo.Merge(commit, new Signature(gitIdentity, DateTimeOffset.Now), standardMergeOptions);

            switch (result.Status)
            {
                case MergeStatus.Conflicts:
                    repo.ResetAndClean();
                    break;
                case MergeStatus.FastForward:
                case MergeStatus.UpToDate:
                    break;
                case MergeStatus.NonFastForward:
                    repo.Commit(message, new Signature(gitIdentity, DateTimeOffset.Now), new Signature(gitIdentity, DateTimeOffset.Now));
                    break;
            }

            return result.Status;
        }

        public static ImmutableDictionary<string, string> GetBranchExpectedOutputCommits(this ReserveScriptParameters parameters) =>
            parameters.BranchDetails.Where(b => b.Value != parameters.Reserve.IncludedBranches[b.Key].LastCommit)
                                                .ToImmutableDictionary(b => b.Key, b => b.Value);

        public static IDictionary<string, string> EnsureOutputBranch(this IDictionary<string, string> original, ReserveScriptParameters parameters, IBranchNaming branchNaming)
        {
            var outputBranches = parameters.Reserve.GetBranchesByRole("Output");
            if (outputBranches.Length == 0)
            {
                var branchName = branchNaming.GetDefaultOutputBranchName(parameters.Name);
                if (!original.ContainsKey(branchName))
                {
                    return original.ToImmutableDictionary()
                        .Add(branchName, BranchReserve.EmptyCommit);
                }
            }
            return original;
        }

        public static Dictionary<string, string> GetReserveExpectedOutputCommits(this ReserveScriptParameters parameters)
        {
            return parameters.UpstreamReserves.Where(r => r.Value.OutputCommit != parameters.Reserve.Upstream[r.Key].LastOutput)
                .ToDictionary(r => r.Key, r => parameters.UpstreamReserves[r.Key].OutputCommit);
        }

    }
}
