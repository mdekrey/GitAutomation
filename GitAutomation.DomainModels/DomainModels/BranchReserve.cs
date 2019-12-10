using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitAutomation.DomainModels
{
    public class BranchReserve
    {
        public static readonly string EmptyCommit = new string('0', 40);

        private readonly ImmutableDictionary<string, object> data;

        private BranchReserve(ImmutableDictionary<string, object> data)
        {
            this.data = data;
        }

        public BranchReserve(string reserveType, string flowType, string status, ImmutableSortedDictionary<string, UpstreamReserve> upstream, ImmutableSortedDictionary<string, BranchReserveBranch> includedBranches, string outputCommit, ImmutableSortedDictionary<string, string> meta)
        {
            if (reserveType == null)
            {
                throw new ArgumentException($"Invalid reserve type '{reserveType}'", nameof(reserveType));
            }
            else if (flowType == null)
            {
                throw new ArgumentException($"Invalid flow type '{flowType}'", nameof(flowType));
            }
            else if (status == null)
            {
                throw new ArgumentException($"Invalid status '{status}'", nameof(status));
            }
            else if (upstream == null)
            {
                throw new ArgumentException($"Upstream list cannot be null", nameof(upstream));
            }
            else if (includedBranches == null)
            {
                throw new ArgumentException($"Included Branches list cannot be null", nameof(includedBranches));
            }
            else if (meta == null)
            {
                throw new ArgumentException($"Meta cannot be null", nameof(meta));
            }
            else if (!Regex.IsMatch(outputCommit.ToLower(), "^[0-9a-f]{40}$"))
            {
                throw new ArgumentException($"Invalid commit '{outputCommit}'", nameof(outputCommit));
            }
            data = new Dictionary<string, object>
            {
                { nameof(ReserveType), reserveType.Trim() },
                { nameof(FlowType), flowType.Trim() },
                { nameof(Status), status.Trim() },
                { nameof(Upstream), upstream },
                { nameof(IncludedBranches), includedBranches },
                { nameof(OutputCommit), outputCommit.ToLower() },
                { nameof(Meta), meta },
            }.ToImmutableDictionary();
        }

        public string ReserveType => (string)data[nameof(ReserveType)];
        public BranchReserve SetReserveType(string value)
        {
            return new BranchReserve(data.SetItem(nameof(ReserveType), value));
        }
        public string FlowType => (string)data[nameof(FlowType)];
        public BranchReserve SetFlowType(string value)
        {
            return new BranchReserve(data.SetItem(nameof(FlowType), value));
        }
        public string Status => (string)data[nameof(Status)];
        public BranchReserve SetStatus(string value)
        {
            return new BranchReserve(data.SetItem(nameof(Status), value));
        }

        public ImmutableSortedDictionary<string, UpstreamReserve> Upstream => (ImmutableSortedDictionary<string, UpstreamReserve>)data[nameof(Upstream)];
        public BranchReserve SetUpstream(Func<ImmutableSortedDictionary<string, UpstreamReserve>, ImmutableSortedDictionary<string, UpstreamReserve>> map)
        {
            return new BranchReserve(data.SetItem(nameof(Upstream), map(Upstream)));
        }

        public ImmutableSortedDictionary<string, BranchReserveBranch> IncludedBranches => (ImmutableSortedDictionary<string, BranchReserveBranch>)data[nameof(IncludedBranches)];
        public BranchReserve SetIncludedBranches(Func<ImmutableSortedDictionary<string, BranchReserveBranch>, ImmutableSortedDictionary<string, BranchReserveBranch>> map)
        {
            return new BranchReserve(data.SetItem(nameof(IncludedBranches), map(IncludedBranches)));
        }

        public string OutputCommit => (string)data[nameof(OutputCommit)];
        public BranchReserve SetOutputCommit(string value)
        {
            return new BranchReserve(data.SetItem(nameof(OutputCommit), value));
        }

        public ImmutableSortedDictionary<string, string> Meta => (ImmutableSortedDictionary<string, string>)data[nameof(Meta)];
        public BranchReserve SetMeta(Func<ImmutableSortedDictionary<string, string>, ImmutableSortedDictionary<string, string>> map)
        {
            return new BranchReserve(data.SetItem(nameof(Meta), map(Meta)));
        }


        public Builder ToBuilder()
        {
            return new Builder(this);
        }

        public class Builder
        {
            private Dictionary<string, UpstreamReserve.Builder>? upstream;
            private Dictionary<string, BranchReserveBranch.Builder>? includedBranches;
            private Dictionary<string, string>? meta;
            private BranchReserve original;

            public Builder() : this(new BranchReserve(
                reserveType: "",
                flowType: "",
                status: "",
                upstream: ImmutableSortedDictionary<string, UpstreamReserve>.Empty,
                includedBranches: ImmutableSortedDictionary<string, BranchReserveBranch>.Empty,
                outputCommit: EmptyCommit,
                meta: ImmutableSortedDictionary<string, string>.Empty))
            {
            }

            public Builder(BranchReserve original)
            {
                this.original = original;
            }

            public string ReserveType
            {
                get => original.ReserveType;
                set { original = original.SetReserveType(value); }
            }
            public string FlowType
            {
                get => original.FlowType;
                set { original = original.SetFlowType(value); }
            }
            public string Status
            {
                get => original.Status;
                set { original = original.SetStatus(value); }
            }
            public Dictionary<string, UpstreamReserve.Builder> Upstream
            {
                get => upstream ??= original.Upstream.ToDictionary(k => k.Key, k => k.Value.ToBuilder());
                set
                {
                    original = original.SetUpstream(_ => value.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value.Build()));
                    upstream = null;
                }
            }
            public Dictionary<string, BranchReserveBranch.Builder> IncludedBranches
            {
                get => includedBranches ??= original.IncludedBranches.ToDictionary(k => k.Key, k => k.Value.ToBuilder());
                set
                {
                    original = original.SetIncludedBranches(_ => value.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value.Build()));
                    includedBranches = null;
                }
            }
            public string OutputCommit
            {
                get => original.OutputCommit;
                set { original = original.SetOutputCommit(value); }
            }
            public Dictionary<string, string> Meta
            {
                get => meta ??= new Dictionary<string, string>(original.Meta);
                set
                {
                    original = original.SetMeta(_ => value.ToImmutableSortedDictionary());
                    meta = null;
                }
            }

            public BranchReserve Build()
            {
                if (upstream != null)
                {
                    Upstream = upstream;
                }
                if (includedBranches != null)
                {
                    IncludedBranches = includedBranches;
                }
                if (meta != null)
                {
                    Meta = meta;
                }

                return original;
            }
        }
    }
}
