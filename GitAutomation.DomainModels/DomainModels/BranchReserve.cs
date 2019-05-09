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

        public BranchReserve(string reserveType, string flowType, string status, ImmutableSortedSet<string> upstream, string lastCommit, ImmutableSortedDictionary<string, object> meta)
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
            else if (!Regex.IsMatch(lastCommit.ToLower(), "^[0-9a-f]{40}$"))
            {
                throw new ArgumentException($"Invalid commit '{lastCommit}'", nameof(lastCommit));
            }
            data = new Dictionary<string, object>
            {
                { nameof(ReserveType), reserveType.Trim() },
                { nameof(FlowType), flowType.Trim() },
                { nameof(Status), status.Trim() },
                { nameof(Upstream), upstream },
                { nameof(LastCommit), lastCommit.ToLower() },
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

        public ImmutableSortedSet<string> Upstream => (ImmutableSortedSet<string>)data[nameof(Upstream)];
        public BranchReserve SetUpstream(Func<ImmutableSortedSet<string>, ImmutableSortedSet<string>> map)
        {
            return new BranchReserve(data.SetItem(nameof(Upstream), map(Upstream)));
        }

        public string LastCommit => (string)data[nameof(LastCommit)];
        public BranchReserve SetLastCommit(string value)
        {
            return new BranchReserve(data.SetItem(nameof(LastCommit), value));
        }

        public ImmutableSortedDictionary<string, object> Meta => (ImmutableSortedDictionary<string, object>)data[nameof(Meta)];
        public BranchReserve SetMeta(Func<ImmutableSortedDictionary<string, object>, ImmutableSortedDictionary<string, object>> map)
        {
            return new BranchReserve(data.SetItem(nameof(Meta), map(Meta)));
        }


        public Builder ToBuilder()
        {
            return new Builder(this);
        }

        public class Builder
        {
            private BranchReserve original;

            public Builder() : this(new BranchReserve("", "", "", ImmutableSortedSet<string>.Empty, EmptyCommit, ImmutableSortedDictionary<string, object>.Empty))
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
            public HashSet<string> Upstream
            {
                get => new HashSet<string>(original.Upstream);
                set { original = original.SetUpstream(_ => value.ToImmutableSortedSet()); }
            }
            public string LastCommit
            {
                get => original.LastCommit;
                set { original = original.SetLastCommit(value); }
            }
            public Dictionary<string, object> Meta
            {
                get => new Dictionary<string, object>(original.Meta);
                set { original = original.SetMeta(_ => value.ToImmutableSortedDictionary()); }
            }

            public BranchReserve Build()
            {
                return original;
            }
        }
    }
}
