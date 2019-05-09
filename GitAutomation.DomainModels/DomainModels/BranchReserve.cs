using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace GitAutomation.DomainModels
{
    public class BranchReserve
    {
        public static readonly string EmptyCommit = new string('0', 40);

        public BranchReserve(string reserveType, string flowType, string status, ImmutableSortedSet<string> upstream, string lastCommit, ImmutableSortedDictionary<string, object> meta)
        {
            if (string.IsNullOrWhiteSpace(reserveType))
            {
                throw new ArgumentException($"Invalid reserve type '{reserveType}'", nameof(reserveType));
            }
            else if (string.IsNullOrWhiteSpace(flowType))
            {
                throw new ArgumentException($"Invalid flow type '{flowType}'", nameof(flowType));
            }
            else if (string.IsNullOrWhiteSpace(status))
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
            ReserveType = reserveType.Trim();
            FlowType = flowType.Trim();
            Status = status.Trim();
            Upstream = upstream;
            LastCommit = lastCommit.ToLower();
            Meta = meta;
        }

        public string ReserveType { get; }
        public string FlowType { get; }
        public string Status { get; }

        public ImmutableSortedSet<string> Upstream { get; }

        public string LastCommit { get; }

        public ImmutableSortedDictionary<string, object> Meta { get; }


        public Builder ToBuilder()
        {
            return new Builder(ReserveType, FlowType, Status, Upstream, LastCommit, Meta);
        }

        public class Builder
        {

            public Builder() : this("", "", "", ImmutableSortedSet<string>.Empty, EmptyCommit, ImmutableSortedDictionary<string, object>.Empty)
            {

            }

            public Builder(string reserveType, string flowType, string status, ImmutableSortedSet<string> upstream, string lastCommit, ImmutableSortedDictionary<string, object> meta)
            {
                ReserveType = reserveType;
                FlowType = flowType;
                Status = status;
                Upstream = new HashSet<string>(upstream);
                LastCommit = lastCommit;
                Meta = new Dictionary<string, object>(meta);
            }

            public string ReserveType { get; set; }
            public string FlowType { get; set; }
            public string Status { get; set; }
            public HashSet<string> Upstream { get; set; }
            public string LastCommit { get; set; }
            public Dictionary<string, object> Meta { get; set; }

            public BranchReserve Build()
            {
                return new BranchReserve(ReserveType, FlowType, Status, Upstream.ToImmutableSortedSet(), LastCommit, Meta.ToImmutableSortedDictionary());
            }
        }
    }
}
