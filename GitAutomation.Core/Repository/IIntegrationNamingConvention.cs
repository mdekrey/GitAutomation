using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Repository
{
    public interface IIntegrationNamingConvention
    {
        IObservable<string> GetIngtegrationBranchNameCandidates(string branchA, string branchB);
    }
}
