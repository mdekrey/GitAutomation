using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Repository
{
    public interface IBranchIterationMediator
    {
        string GetLatestBranchNameIteration(string branchName, IEnumerable<string> existingNames);

        bool IsBranchIteration(string originalName, string candidateName);

        Task<string> GetNextBranchNameIteration(string branchName, IEnumerable<string> existingNames);

        string GuessBranchIterationRoot(string branchName);
    }
}
