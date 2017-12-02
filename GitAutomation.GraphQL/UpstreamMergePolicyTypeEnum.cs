using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.GraphQL
{
    class UpstreamMergePolicyTypeEnum : EnumerationGraphType<BranchSettings.UpstreamMergePolicy>
    {
        public UpstreamMergePolicyTypeEnum()
        {
            Name = "UpstreamMergePolicy";
            Description = "Special handling merge policy when upstream branches are needed.";

            foreach (var value in this.Values)
            {
                value.Name = ((BranchSettings.UpstreamMergePolicy)value.Value).ToString("g");
            }
        }
    }
}
