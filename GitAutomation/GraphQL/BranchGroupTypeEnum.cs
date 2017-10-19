using GraphQL.Types;
using System;

namespace GitAutomation.GraphQL
{
    internal class BranchGroupTypeEnum : EnumerationGraphType<BranchSettings.BranchGroupType>
    {
        public BranchGroupTypeEnum()
        {
            Name = "BranchGroupType";
            Description = "Type of the Branch Group, according to Scaled Git Flow";
            
            foreach (var value in this.Values)
            {
                value.Name = ((BranchSettings.BranchGroupType)value.Value).ToString("g");
            }
        }
    }
}