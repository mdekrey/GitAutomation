using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.BranchSettings
{
    public class Consolidation
    {
        // Something was detected as being wrong in the remaining calculation below. Block and do not proceed
        public bool Disallow { get; set; }

        // Key is the upstream branch, Value are the downstream branches
        public ImmutableDictionary<string, ImmutableList<string>> AddToLinks { get; set; }
        public ImmutableDictionary<string, ImmutableList<string>> RemoveFromLinks { get; set; }
    }
}
