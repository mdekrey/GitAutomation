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
            public Builder SetBranchReserves(Dictionary<string, BranchReserve.Builder> branchReserves)
            {
                BranchReserves = branchReserves;
                return this;
            }

            public RepositoryStructure Build()
            {
                return new RepositoryStructure(BranchReserves.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value.Build()));
            }
        }
    }
}
