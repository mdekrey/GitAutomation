using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitAutomation.DomainModels
{
    public static class BranchReserveExtensions
    {
        public static string[] GetBranchesByRole(this BranchReserve reserve, string role)
        {
            return reserve.IncludedBranches.Keys.Where(k => reserve.IncludedBranches[k].Meta["Role"] == role).ToArray();
        }

    }
}
