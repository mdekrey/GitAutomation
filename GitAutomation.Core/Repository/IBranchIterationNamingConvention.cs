using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Repository
{
    public interface IBranchIterationNamingConvention
    {
        bool IsBranchIteration(string originalName, string candidateName);

        IObservable<string> GetBranchNameIterations(string branchName);
    }
}
