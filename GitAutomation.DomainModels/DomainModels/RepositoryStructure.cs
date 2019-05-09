using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace GitAutomation.DomainModels
{
    public class RepositoryStructure
    {
        public RepositoryStructure(ImmutableSortedDictionary<string, BranchReserve> branchReserves)
        {
            BranchReserves = branchReserves;
        }

        public ImmutableSortedDictionary<string, BranchReserve> BranchReserves { get; }

        public IEnumerable<ValidationError> GetValidationErrors()
        {
            var upstreamAllValid = true;
            foreach (var reserve in BranchReserves)
            {
                foreach (var upstream in reserve.Value.Upstream)
                {
                    if (!BranchReserves.ContainsKey(upstream))
                    {
                        upstreamAllValid = false;
                        yield return new ValidationError("ReserveUpstreamInvalid")
                        {
                            Arguments = { { "reserve", reserve.Key }, { "upstream", upstream } }
                        };
                    }
                }
            }

            if (upstreamAllValid)
            {
                foreach (var cycle in Cycle.FindAllCycles(this).Select(c => c.ToString()).OrderBy(t => t))
                {
                    yield return new ValidationError("ReserveCycleDetected")
                    {
                        Arguments = { { "cycle", cycle.ToString() } }
                    };
                }
            }
        }

        public Builder ToBuilder()
        {
            return new Builder(BranchReserves);
        }

        public class Builder
        {
            public Builder() : this(branchReserves: ImmutableSortedDictionary<string, BranchReserve>.Empty) { }

            public Builder(IReadOnlyDictionary<string, BranchReserve> branchReserves)
            {
                this.BranchReserves = branchReserves.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToBuilder());
            }

            public Dictionary<string, BranchReserve.Builder> BranchReserves { get; set; }

            public RepositoryStructure Build()
            {
                return new RepositoryStructure(BranchReserves.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value.Build()));
            }
        }
    }
}
