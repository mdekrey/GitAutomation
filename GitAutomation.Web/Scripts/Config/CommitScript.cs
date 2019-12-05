using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Configuration.Actions;
using GitAutomation.Scripting;
using GitAutomation.Web;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Config
{
    public class CommitScript : IScript<CommitScript.CommitScriptParams>
    {
        private readonly ConfigRepositoryOptions configRepositoryOptions;
        private readonly IDispatcher dispatcher;

        public class CommitScriptParams
        {
            public CommitScriptParams(DateTimeOffset startTimestamp, string comment)
            {
                StartTimestamp = startTimestamp;
                Comment = comment;
            }
            public DateTimeOffset StartTimestamp { get; }
            public string Comment { get; set; }
        }

        public CommitScript(IOptions<ConfigRepositoryOptions> options, IDispatcher dispatcher)
        {
            this.configRepositoryOptions = options.Value;
            this.dispatcher = dispatcher;
        }

        public async Task Run(CommitScriptParams parameters, ILogger logger, IAgentSpecification agent)
        {
            await Task.Yield();
            var startTimestamp = parameters.StartTimestamp;

            using var repo = new LibGit2Sharp.Repository(configRepositoryOptions.CheckoutPath);
            Commands.Stage(repo, "*");
            var author = new Signature(configRepositoryOptions.UserName, configRepositoryOptions.UserEmail, DateTimeOffset.Now);
            try
            {
                repo.Commit(parameters.Comment, author, author);
            }
            catch (Exception ex)
            {
                dispatcher.Dispatch(new GitCouldNotCommitAction { StartTimestamp = startTimestamp }, agent, $"Failed to commit changes\n\n{ex.ToString()}");
            }
        }
    }
}
