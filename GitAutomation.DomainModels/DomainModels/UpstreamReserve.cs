using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace GitAutomation.DomainModels
{
    public class UpstreamReserve
    {
        private readonly ImmutableDictionary<string, object> data;

        public static readonly UpstreamReserve Default = new UpstreamReserve(BranchReserve.EmptyCommit);

        private UpstreamReserve(ImmutableDictionary<string, object> data)
        {
            this.data = data;
        }

        public UpstreamReserve(string lastOutput, string role = "Source", ImmutableSortedDictionary<string, string>? meta = null)
        {
            var resultMeta = meta ?? ImmutableSortedDictionary<string, string>.Empty;
            if (lastOutput == null)
            {
                throw new ArgumentException($"Commit cannot be null '{lastOutput}'", nameof(lastOutput));
            }
            else if (!Regex.IsMatch(lastOutput.ToLower(), "^[0-9a-f]{40}$"))
            {
                throw new ArgumentException($"Invalid commit '{lastOutput}'", nameof(lastOutput));
            }
            else if (string.IsNullOrEmpty(role))
            {
                throw new ArgumentException($"Role cannot be null or empty '{role}'", nameof(role));
            }
            data = new Dictionary<string, object>
            {
                { nameof(LastOutput), lastOutput.ToLower() },
                { nameof(Role), role },
                { nameof(Meta), resultMeta },
            }.ToImmutableDictionary();
        }

        public string LastOutput => (string)data[nameof(LastOutput)];
        public UpstreamReserve SetLastOutput(string value)
        {
            return new UpstreamReserve(data.SetItem(nameof(LastOutput), value));
        }

        public string Role => (string)data[nameof(Role)];
        public UpstreamReserve SetRole(string value)
        {
            return new UpstreamReserve(data.SetItem(nameof(Role), value));
        }

        public ImmutableSortedDictionary<string, string> Meta => (ImmutableSortedDictionary<string, string>)data[nameof(Meta)];
        public UpstreamReserve SetMeta(Func<ImmutableSortedDictionary<string, string>, ImmutableSortedDictionary<string, string>> map)
        {
            return new UpstreamReserve(data.SetItem(nameof(Meta), map(Meta)));
        }

        public Builder ToBuilder()
        {
            return new Builder(this);
        }

        public class Builder
        {
            private Dictionary<string, string>? meta;
            private UpstreamReserve original;

            public Builder() : this(Default)
            {
            }

            public Builder(UpstreamReserve original)
            {
                this.original = original;
            }

            public string LastOutput
            {
                get => original.LastOutput;
                set { original = original.SetLastOutput(value); }
            }
            public string Role
            {
                get => original.Role;
                set { original = original.SetRole(value); }
            }
            public Dictionary<string, string> Meta
            {
                get => meta = meta ?? new Dictionary<string, string>(original.Meta);
                set
                {
                    original = original.SetMeta(_ => value.ToImmutableSortedDictionary());
                    meta = null;
                }
            }


            public UpstreamReserve Build()
            {
                if (meta != null)
                {
                    Meta = meta;
                }

                return original;
            }
        }
    }
}
