using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Scripts.Branches.Common
{
    public readonly ref struct MultiMergeStatus
    {
        public readonly bool AutomaticallyUpdated;
        public readonly ImmutableHashSet<string> ManualUpstream;

        public MultiMergeStatus(bool hadAutomaticUpdate)
            : this(hadAutomaticUpdate, ImmutableHashSet<string>.Empty)
        {
        }

        public MultiMergeStatus(bool hadAutomaticUpdate, ImmutableHashSet<string> manualUpstream)
        {
            this.AutomaticallyUpdated = hadAutomaticUpdate;
            this.ManualUpstream = manualUpstream;
        }

        public MultiMergeStatus HadAutomaticUpdate()
        {
            return new MultiMergeStatus(true, ManualUpstream);
        }

        public MultiMergeStatus AddManualUpstream(string manualUpstream)
        {
            return new MultiMergeStatus(AutomaticallyUpdated, ManualUpstream.Add(manualUpstream));
        }

        public void Deconstruct(out bool automaticallyUpdated, out ImmutableHashSet<string> manualUpstream)
        {
            automaticallyUpdated = AutomaticallyUpdated;
            manualUpstream = ManualUpstream;
        }
    }
}
