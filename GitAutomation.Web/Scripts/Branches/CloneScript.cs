using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Actions;
using GitAutomation.Scripting;
using GitAutomation.State;
using GitAutomation.Web;
using GitAutomation.Web.State;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Branches
{
    public class CloneScript : IScript<CloneScript.CloneScriptParams>
    {
        private readonly TargetRepositoryOptions targetRepositoryOptions;
        private readonly IDispatcher dispatcher;

        public class CloneScriptParams
        {
            public CloneScriptParams(DateTimeOffset startTimestamp)
            {
                this.StartTimestamp = startTimestamp;
            }

            public DateTimeOffset StartTimestamp { get; }
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
            // 1. Already cloned 
            // 3. Directory exists with no clone with permissions
            // 4. Directory exists with no clone without permissions
            // 5. No clone

            var targetDirectory = CreateDirectory();
            if (targetDirectory == null)
            {
                dispatcher.Dispatch(new DirectoryNotAccessibleAction { StartTimestamp = startTimestamp }, agent, "Could not mirror target; check permissions");
                return;
            }

            if (!Repository.IsValid(targetRepositoryOptions.CheckoutPath))
            {
                var nested = Repository.Discover(targetRepositoryOptions.CheckoutPath);
                if (nested != null)
                {
                    dispatcher.Dispatch(new GitNestedAction { StartTimestamp = startTimestamp, Path = nested }, agent, "The target folder is already inside a git folder");
                    return;
                }

                if (targetDirectory.EnumerateFiles("*", SearchOption.AllDirectories).Any())
                {
                    dispatcher.Dispatch(new GitDirtyAction { StartTimestamp = startTimestamp }, agent, "Could not mirror target; the target directory was not empty");
                    return;
                }

                try { Repository.Init(targetRepositoryOptions.CheckoutPath, isBare: true); }
                catch (Exception ex)
                {
                    dispatcher.Dispatch(new GitCouldNotBeInitializedAction { StartTimestamp = startTimestamp }, agent, $"Could not mirror target; check permissions.\n\n{ex.ToString()}");
                    return;
                }
            }

            using var repo = new Repository(targetRepositoryOptions.CheckoutPath);
            foreach (var remote in targetRepositoryOptions.Remotes)
            {
                try
                {
                    var actualRemote = repo.Network.Remotes[remote.Key];
                    if (actualRemote == null)
                    {
                        actualRemote = repo.Network.Remotes.Add(remote.Key, remote.Value.Url);
                    }
                    repo.Network.Remotes.Update(remote.Key, r =>
                    {
                        r.Url = r.PushUrl = remote.Value.Url;
                        r.FetchRefSpecs.Clear();
                        r.FetchRefSpecs.Add($"+refs/heads/*:refs/heads/{remote.Key}/*");
                        r.TagFetchMode = LibGit2Sharp.TagFetchMode.None;
                    });

                    repo.Network.Fetch(remote.Key, new[] { $"+refs/heads/*:refs/heads/{remote.Key}/*" }, new LibGit2Sharp.FetchOptions
                    {
                        Prune = true,
                        TagFetchMode = LibGit2Sharp.TagFetchMode.None,
                        CredentialsProvider = remote.Value.ToCredentialsProvider()
                    });
                }
                catch (Exception ex)
                {
                    dispatcher.Dispatch(new GitPasswordIncorrectAction { StartTimestamp = startTimestamp, Remote = remote.Key }, agent, $"Could not fetch from {remote.Key}; make sure your repository url and credentials are correct.\n\n{ex.ToString()}");
                }
            }

            dispatcher.Dispatch(new FetchedAction { StartTimestamp = startTimestamp }, agent);
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
