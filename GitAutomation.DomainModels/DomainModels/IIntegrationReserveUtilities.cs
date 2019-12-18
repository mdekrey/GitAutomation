using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.DomainModels
{
    public interface IIntegrationReserveUtilities
    {
        IEnumerable<IStandardAction> AddUpstreamConflicts(string name, List<(string, string)> conflictingUpstream, ImmutableDictionary<string, BranchReserve> upstreamReserves);
    }
}
