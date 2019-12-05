using GitAutomation.DomainModels;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;

namespace GitAutomation.Web
{
    public readonly struct ReserveFullState
    {
        public ReserveFullState(BranchReserve reserve, IDictionary<string, string> branchDetails, IDictionary<string, BranchReserve> upstreamReserves)
        {
            Reserve = reserve;
            BranchDetails = branchDetails.ToImmutableDictionary();
            UpstreamReserves = upstreamReserves.ToImmutableDictionary();
        }

        public BranchReserve Reserve { get; }
        public ImmutableDictionary<string, string> BranchDetails { get; }
        public ImmutableDictionary<string, BranchReserve> UpstreamReserves { get; }
        public bool IsValid => !UpstreamReserves.Values.Any(r => r == null);
    }
}
