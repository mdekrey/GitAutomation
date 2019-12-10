using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace GitAutomation.DomainModels
{
    public class BranchReserveBranch
    {
        private readonly ImmutableDictionary<string, object> data;

        private BranchReserveBranch(ImmutableDictionary<string, object> data)
        {
            this.data = data;
        }

        public BranchReserveBranch(string lastCommit, ImmutableSortedDictionary<string, string>? meta = null)
        {
            var resultMeta = meta ?? ImmutableSortedDictionary<string, string>.Empty;
            if (lastCommit == null)
            {
                throw new ArgumentException($"Commit cannot be null '{lastCommit}'", nameof(lastCommit));
            }
            else if (!Regex.IsMatch(lastCommit.ToLower(), "^[0-9a-f]{40}$"))
            {
                throw new ArgumentException($"Invalid commit '{lastCommit}'", nameof(lastCommit));
            }
            data = new Dictionary<string, object>
            {
                { nameof(LastCommit), lastCommit.ToLower() },
                { nameof(Meta), resultMeta },
            }.ToImmutableDictionary();
        }

        public string LastCommit => (string)data[nameof(LastCommit)];
        public BranchReserveBranch SetLastCommit(string value)
        {
            return new BranchReserveBranch(data.SetItem(nameof(LastCommit), value));
        }

        public ImmutableSortedDictionary<string, string> Meta => (ImmutableSortedDictionary<string, string>)data[nameof(Meta)];
        public BranchReserveBranch SetMeta(Func<ImmutableSortedDictionary<string, string>, ImmutableSortedDictionary<string, string>> map)
        {
            return new BranchReserveBranch(data.SetItem(nameof(Meta), map(Meta)));
        }

        public Builder ToBuilder()
        {
            return new Builder(this);
        }

        public class Builder
        {
            private Dictionary<string, string>? meta;
            private BranchReserveBranch original;

            public Builder() : this(new BranchReserveBranch(BranchReserve.EmptyCommit, ImmutableSortedDictionary<string, string>.Empty))
            {
            }

            public Builder(BranchReserveBranch original)
            {
                this.original = original;
            }

            public string LastCommit
            {
                get => original.LastCommit;
                set { original = original.SetLastCommit(value); }
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


            public BranchReserveBranch Build()
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
