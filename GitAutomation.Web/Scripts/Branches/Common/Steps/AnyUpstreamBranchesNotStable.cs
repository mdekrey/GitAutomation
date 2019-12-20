using GitAutomation.DomainModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Branches.Common.Steps
{
    public class AnyUpstreamBranchesNotStable
    {
        public static bool AreAllUpstreamBranchesStable(ImmutableDictionary<string, BranchReserve> upstreamBranchReserves, ILogger logger)
        {
            if (upstreamBranchReserves.Any(r => r.Value.Status != "Stable"))
            {
                logger.LogInformation($"Had upstream reserves ({string.Join(", ", upstreamBranchReserves.Where(r => r.Value.Status != "Stable").Select(r => r.Key))}) in non-Stable state. Deferring.", upstreamBranchReserves.Where(r => r.Value.Status != "Stable").ToArray());
                return false;
            }
            return true;
        }
    }
}
