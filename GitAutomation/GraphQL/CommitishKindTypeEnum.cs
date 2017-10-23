using GraphQL.Types;
using System;

namespace GitAutomation.GraphQL
{
    internal class CommitishKindTypeEnum : EnumerationGraphType<CommitishKind>
    {
        public CommitishKindTypeEnum()
        {
            Name = "CommitishKind";
            Description = "Type of the commitish provided, whether a branch group, remote branch, or a commit itself";
            
            foreach (var value in this.Values)
            {
                value.Name = ((CommitishKind)value.Value).ToString("g");
            }
        }
    }
}