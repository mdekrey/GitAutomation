using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Repository
{
    public interface IIntegrationNamingMediator
    {
        Task<string> GetIntegrationBranchName(string branchA, string branchB);
    }
}
