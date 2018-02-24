using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation
{
    public static class RepositoryMediatorExtensions
    {
        public static async Task<bool> IsBadBranch(this IRepositoryMediator repository, string branchName)
        {
            return (await repository.GetBadBranchInfo(branchName)) != null;
        }
    }
}
