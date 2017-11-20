using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.GraphQL
{
    class ProcessStateTypeEnum : EnumerationGraphType<CommitishKind>
    {
        public ProcessStateTypeEnum()
        {
            Name = "ProcessState";

            foreach (var value in this.Values)
            {
                value.Name = ((CommitishKind)value.Value).ToString("g");
            }
        }
    }
}
