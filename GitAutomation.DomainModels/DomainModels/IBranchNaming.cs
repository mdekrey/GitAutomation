using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.DomainModels
{
    public interface IBranchNaming
    {
        string GetCheckoutRepositoryBranchName(string remoteName, string branchName);
        (string remoteName, string branchName) SplitCheckoutRepositoryBranchName(string checkoutRepositoryBranchName);

        string GenerateIntegrationBranchName(params string[] reserveNames);
        string GetDefaultOutputBranchName(string reserveName);
    }
}
