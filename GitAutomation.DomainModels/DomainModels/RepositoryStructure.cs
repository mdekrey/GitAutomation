using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace GitAutomation.DomainModels
{
    public class RepositoryStructure
    {
        private readonly ImmutableDictionary<string, object> data;

        private RepositoryStructure(ImmutableDictionary<string, object> data)
        {
            this.data = data;
        }

        public RepositoryStructure(ImmutableSortedDictionary<string, BranchReserve> branchReserves)
        {
            data = new Dictionary<string, object>
            {
                { nameof(BranchReserves), branchReserves },
            }.ToImmutableDictionary();
        }

        public ImmutableSortedDictionary<string, BranchReserve> BranchReserves => (ImmutableSortedDictionary<string, BranchReserve>)data[nameof(BranchReserves)];
        public RepositoryStructure SetBranchReserves(Func<ImmutableSortedDictionary<string, BranchReserve>, ImmutableSortedDictionary<string, BranchReserve>> map)
        {
            return new RepositoryStructure(data.SetItem(nameof(BranchReserves), map(BranchReserves)));
        }

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
            return new Builder(this);
        }

        public class Builder
        {
            private RepositoryStructure original;
            public Builder(RepositoryStructure original)
            {
                this.original = original;
            }

            public Builder() : this(new RepositoryStructure(branchReserves: ImmutableSortedDictionary<string, BranchReserve>.Empty)) { }

            public Dictionary<string, BranchReserve.Builder> BranchReserves
            {
                get => original.BranchReserves.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToBuilder()) ;
                set { original = original.SetBranchReserves(_ => value.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value.Build())); }
            }

            public RepositoryStructure Build()
            {
                return new RepositoryStructure(BranchReserves.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value.Build()));
            }
        }
    }
}
