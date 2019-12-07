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
        private readonly ConfigRepositoryOptions configRepositoryOptions;
        private readonly IDispatcher dispatcher;

        public class CloneScriptParams
        {
            public CloneScriptParams(DateTimeOffset startTimestamp)
            {
                StartTimestamp = startTimestamp;
            }
            public DateTimeOffset StartTimestamp { get; }
        }

        public CloneScript(IOptions<ConfigRepositoryOptions> options, IDispatcher dispatcher)
        {
            this.configRepositoryOptions = options.Value;
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

            if (!LibGit2Sharp.Repository.IsValid(configRepositoryOptions.CheckoutPath))
            {
                var nested = LibGit2Sharp.Repository.Discover(configRepositoryOptions.CheckoutPath);
                if (nested != null)
                {
                    dispatcher.Dispatch(new GitNestedAction { StartTimestamp = startTimestamp, Path = nested }, agent, "The target folder is already inside a git folder");
                    return;
                }

                if (CloneRepositoryConfiguration())
                {
                    dispatcher.Dispatch(new ReadyToLoadAction { StartTimestamp = startTimestamp }, agent, "Ready to load configuration from disk");
                    return;
                }

                if (targetDirectory.EnumerateFiles("*", SearchOption.AllDirectories).Any())
                {
                    dispatcher.Dispatch(new GitCouldNotCloneAction { StartTimestamp = startTimestamp }, agent, "Could not clone configuration; the target directory was not empty");
                    return;
                }

                try { LibGit2Sharp.Repository.Init(configRepositoryOptions.CheckoutPath, isBare: false); }
                catch (Exception ex)
                {
                    dispatcher.Dispatch(new GitCouldNotCloneAction { StartTimestamp = startTimestamp }, agent, $"Could not initialize configuration repository; git init failed.\n\n{ex.ToString()}");
                    return;
                }
                using var repoTemp = new LibGit2Sharp.Repository(configRepositoryOptions.CheckoutPath);
                repoTemp.Network.Remotes.Add("origin", configRepositoryOptions.Repository);
            }

            using var repo = new LibGit2Sharp.Repository(configRepositoryOptions.CheckoutPath);
            repo.Network.Remotes.Remove("origin");
            repo.Network.Remotes.Add("origin", configRepositoryOptions.Repository);
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
                Commands.Checkout(repo, repo.Branches[$"origin/{configRepositoryOptions.BranchName}"]);
            }
            catch (Exception ex)
            {
                try { Commands.Checkout(repo, repo.Head.Tip); } catch { }
                if (repo.Branches[configRepositoryOptions.BranchName] != null)
                {
                    repo.Branches.Remove(configRepositoryOptions.BranchName);
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

        private bool CloneRepositoryConfiguration()
        {
            try
            {
                LibGit2Sharp.Repository.Clone(configRepositoryOptions.Repository, configRepositoryOptions.CheckoutPath, new CloneOptions
                {
                    IsBare = false,
                    BranchName = configRepositoryOptions.BranchName,
                    // CredentialsProvider = // TODO - passwords
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
                var targetDirectory = System.IO.Directory.CreateDirectory(configRepositoryOptions.CheckoutPath);
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
