using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Configuration.Actions;
using GitAutomation.Scripting;
using GitAutomation.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Config
{
    public class PushScript : IScript<PushScript.PushScriptParams>
    {
        private readonly ConfigRepositoryOptions configRepositoryOptions;
        private readonly IDispatcher dispatcher;

        public class PushScriptParams
        {
            public PushScriptParams(DateTimeOffset startTimestamp)
            {
                StartTimestamp = startTimestamp;
            }
            public DateTimeOffset StartTimestamp { get; }
        }

        public PushScript(IOptions<ConfigRepositoryOptions> options, IDispatcher dispatcher)
        {
            this.configRepositoryOptions = options.Value;
            this.dispatcher = dispatcher;
        }

        public async Task Run(PushScriptParams parameters, ILogger logger, IAgentSpecification agent)
        {
            await Task.Yield();
            using var repo = new LibGit2Sharp.Repository(configRepositoryOptions.CheckoutPath);

            try
            {
                repo.Network.Push(repo.Network.Remotes["origin"], $"HEAD:refs/heads/{configRepositoryOptions.BranchName}", new LibGit2Sharp.PushOptions
                {
                    CredentialsProvider = configRepositoryOptions.Repository.ToCredentialsProvider()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to push configuration to remote");
                dispatcher.Dispatch(new GitCouldNotPushAction { StartTimestamp = parameters.StartTimestamp }, agent, "Failed to push changes");
                return;
            }
            dispatcher.Dispatch(new GitPushSuccessAction { StartTimestamp = parameters.StartTimestamp }, agent, "Pushed changes");
            return;
        }
    }
}
