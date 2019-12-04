using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Configuration.Actions;
using GitAutomation.Scripting;
using GitAutomation.Web;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Config
{
    public class CloneScript : IScript<CloneScript.CloneScriptParams>
    {
        private readonly TargetRepositoryOptions targetRepositoryOptions;
        private readonly IDispatcher dispatcher;

        public class CloneScriptParams
        {
            public CloneScriptParams(DateTimeOffset startTimestamp, string branchName)
            {
                StartTimestamp = startTimestamp;
                BranchName = branchName;
            }
            public DateTimeOffset StartTimestamp { get; }
            public string BranchName { get; }
        }

        public CloneScript(IOptions<TargetRepositoryOptions> options, IDispatcher dispatcher)
        {
            this.targetRepositoryOptions = options.Value;
            this.dispatcher = dispatcher;
        }

        public async Task Run(CloneScriptParams parameters, ILogger logger, IAgentSpecification agent)
        {
            await Task.Yield();
            var startTimestamp = parameters.StartTimestamp;
            // Could be in the following states:
            // 1. Already cloned with correct branch
            // 2. Already cloned but wrong branch and dirty working directory
            // 3. Directory exists with no checkout with permissions
            // 4. Directory exists with no checkout without permissions
            // 5. No checkout

            var targetDirectory = CreateDirectory();
            if (targetDirectory == null)
            {
                dispatcher.Dispatch(new DirectoryNotAccessibleAction { StartTimestamp = startTimestamp }, agent, "Could not mirror target; check permissions");
                return;
            }

            if (!LibGit2Sharp.Repository.IsValid(targetRepositoryOptions.CheckoutPath))
            {
                var nested = LibGit2Sharp.Repository.Discover(targetRepositoryOptions.CheckoutPath);
                if (nested != null)
                {
                    dispatcher.Dispatch(new GitNestedAction { StartTimestamp = startTimestamp, Path = nested }, agent, "The target folder is already inside a git folder");
                    return;
                }

                if (CloneRepositoryConfiguration(parameters.BranchName))
                {
                    dispatcher.Dispatch(new ReadyToLoadAction { StartTimestamp = startTimestamp }, agent, "Ready to load configuration from disk");
                    return;
                }

                if (targetDirectory.EnumerateFiles("*", SearchOption.AllDirectories).Any())
                {
                    dispatcher.Dispatch(new GitCouldNotCloneAction { StartTimestamp = startTimestamp }, agent, "Could not clone configuration; the target directory was not empty");
                    return;
                }

                try { LibGit2Sharp.Repository.Init(targetRepositoryOptions.CheckoutPath, isBare: true); }
                catch (Exception ex)
                {
                    dispatcher.Dispatch(new GitCouldNotCloneAction { StartTimestamp = startTimestamp }, agent, $"Could not initialize configuration repository; git init failed.\n\n{ex.ToString()}");
                    return;
                }
                using var repoTemp = new LibGit2Sharp.Repository(targetRepositoryOptions.CheckoutPath);
                repoTemp.Network.Remotes.Add("origin", targetRepositoryOptions.Repository);
            }

            using var repo = new LibGit2Sharp.Repository(targetRepositoryOptions.CheckoutPath);
            repo.Network.Remotes.Remove("origin");
            repo.Network.Remotes.Add("origin", targetRepositoryOptions.Repository);
            try
            {
                repo.Network.Fetch("origin", Enumerable.Empty<string>(), new FetchOptions
                {
                    Prune = true,
                    TagFetchMode = LibGit2Sharp.TagFetchMode.None,
                    // CredentialsProvider = // TODO - password
                });
            }
            catch (Exception ex)
            {
                dispatcher.Dispatch(new GitPasswordIncorrectAction { StartTimestamp = startTimestamp }, agent, $"Could not initialize configuration repository; git init failed.\n\n{ex.ToString()}");
                return;
            }

            try
            {
                Commands.Checkout(repo, repo.Branches[$"origin/{parameters.BranchName}"]);
            }
            catch (Exception ex)
            {
                try { Commands.Checkout(repo, repo.Head.Tip); } catch { }
                if (repo.Branches[parameters.BranchName] != null)
                {
                    repo.Branches.Remove(parameters.BranchName);
                }
                repo.Refs.UpdateTarget("HEAD", "refs/heads/git-config");
                try { repo.Reset(ResetMode.Hard); } catch { }
                // We used to `git clean -fxd`, but I don't see an equivalent?

                // Remove all refs for better gc
                foreach (var branch in repo.Refs.Select(r => r.CanonicalName).ToArray())
                {
                    repo.Refs.Remove(branch);
                }

                dispatcher.Dispatch(new GitNoBranchAction { StartTimestamp = startTimestamp }, agent, $"Configuration branch could not be found");
                return;
            }

            dispatcher.Dispatch(new ReadyToLoadAction { StartTimestamp = startTimestamp }, agent, "Ready to load configuration from disk");
        }

        private bool CloneRepositoryConfiguration(string branchName)
        {
            try
            {
                LibGit2Sharp.Repository.Clone(targetRepositoryOptions.Repository, targetRepositoryOptions.CheckoutPath, new CloneOptions
                {
                    BranchName = branchName
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private DirectoryInfo? CreateDirectory()
        {
            try
            {
                var targetDirectory = System.IO.Directory.CreateDirectory(targetRepositoryOptions.CheckoutPath);
                return targetDirectory.Exists
                    ? targetDirectory
                    : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
