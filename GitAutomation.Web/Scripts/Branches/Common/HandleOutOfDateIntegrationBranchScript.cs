using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Actions;
using GitAutomation.Scripting;
using GitAutomation.Web;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Branches.Common
{
    public class HandleOutOfDateIntegrationBranchScript : IScript<ReserveScriptParameters>
    {
        private readonly IDispatcher dispatcher;
        private readonly IBranchNaming branchNaming;
        private readonly IIntegrationReserveUtilities integrationReserveUtilities;
        private readonly TargetRepositoryOptions options;
        private readonly AutomationOptions automationOptions;
        protected static readonly MergeOptions standardMergeOptions = new MergeOptions
        {
            CommitOnSuccess = false
        };

        public HandleOutOfDateIntegrationBranchScript(IDispatcher dispatcher, IOptions<TargetRepositoryOptions> options, IOptions<AutomationOptions> automationOptions, IBranchNaming branchNaming, IIntegrationReserveUtilities integrationReserveUtilities)
        {
            this.dispatcher = dispatcher;
            this.branchNaming = branchNaming;
            this.integrationReserveUtilities = integrationReserveUtilities;
            this.options = options.Value;
            this.automationOptions = automationOptions.Value;
        }

        public async Task Run(ReserveScriptParameters parameters, ILogger logger, IAgentSpecification agent)
        {
            await Task.Yield();

        }

    }
}
