using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Repository
{
    public interface IBranchIterationNamingConvention
    {
        string GetLatestBranchNameIteration(string branchName, IEnumerable<string> existingNames);

        bool IsBranchIteration(string originalName, string candidateName);

        IObservable<string> GetBranchNameIterations(string branchName, IEnumerable<string> existingNames);

        IComparer<string> GetIterationNameComparer(string name);
    }
}
