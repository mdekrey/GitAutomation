using GraphQL.Types;
using static GitAutomation.GitService.CommitStatus;

namespace GitAutomation.GraphQL
{
    internal class CommitStatusStateTypeEnum : EnumerationGraphType<StatusState>
    {
        public CommitStatusStateTypeEnum()
        {
            Name = "StatusState";
            Description = "State of the status check";

            foreach (var value in this.Values)
            {
                value.Name = ((StatusState)value.Value).ToString("g");
            }
        }
    }
}