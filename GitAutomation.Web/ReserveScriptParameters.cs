using GitAutomation.DomainModels;
using System.Collections.Immutable;

namespace GitAutomation.Web
{
    public class ReserveScriptParameters
    {

        public ReserveScriptParameters(string name, BranchReserve reserve, ImmutableDictionary<string, string> branchDetails, ImmutableDictionary<string, BranchReserve> upstreamReserves, string workingPath)
        {
            this.Name = name;
            this.Reserve = reserve;
            this.BranchDetails = branchDetails;
            this.UpstreamReserves = upstreamReserves;
            this.WorkingPath = workingPath;
        }

        public string Name { get; }
        public BranchReserve Reserve { get; }
        public ImmutableDictionary<string, string> BranchDetails { get; }
        public ImmutableDictionary<string, BranchReserve> UpstreamReserves { get; }
        public string WorkingPath { get; }
    }
}