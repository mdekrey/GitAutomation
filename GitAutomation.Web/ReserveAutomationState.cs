using GitAutomation.DomainModels;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;

namespace GitAutomation.Web
{
    readonly struct ReserveAutomationState
    {
        public ReserveAutomationState(BranchReserve reserve, ImmutableDictionary<string, string> branchDetails, ImmutableDictionary<string, BranchReserve> upstreamReserves)
        {
            Reserve = reserve;
            BranchDetails = branchDetails;
            UpstreamReserves = upstreamReserves;
        }

        public BranchReserve Reserve { get; }
        public ImmutableDictionary<string, string> BranchDetails { get; }
        public ImmutableDictionary<string, BranchReserve> UpstreamReserves { get; }
        public bool IsValid => !UpstreamReserves.Values.Any(r => r == null);
    }
}
