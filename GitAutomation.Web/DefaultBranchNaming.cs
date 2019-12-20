using GitAutomation.DomainModels;
using GitAutomation.Web;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Web
{
    public class DefaultBranchNaming : IBranchNaming
    {
        private readonly AutomationOptions automationOptions;

        public DefaultBranchNaming(IOptions<AutomationOptions> automationOptions)
        {
            this.automationOptions = automationOptions.Value;
        }

        public string GenerateIntegrationBranchName(params string[] reserveNames)
        {
            // TODO - handle conflicts
            // TODO - check max branch name length
            // Do not use "/" because it can cause conflicts with existing file names.
            return automationOptions.IntegrationPrefix + string.Join('_', reserveNames);
        }

        public string GetDefaultOutputBranchName(string reserveName) =>
            GetCheckoutRepositoryBranchName(automationOptions.DefaultRemote, reserveName);

        public string GetCheckoutRepositoryBranchName(string remoteName, string branchName) =>
            $"{remoteName}/{branchName}";

        public (string remoteName, string branchName) SplitCheckoutRepositoryBranchName(string checkoutRepositoryBranchName)
        {
            var branchParts = checkoutRepositoryBranchName.Split('/', 2);
            return (remoteName: branchParts[0], branchName: branchParts[1]);
        }
    }
}
