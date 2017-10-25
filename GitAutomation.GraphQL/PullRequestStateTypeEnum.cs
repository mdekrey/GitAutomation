using GitAutomation.GitService;
using GraphQL.Types;

namespace GitAutomation.GraphQL
{
    internal class PullRequestStateTypeEnum : EnumerationGraphType<PullRequestState>
    {
        public PullRequestStateTypeEnum()
        {
            Name = "PullRequestState";
            Description = "State of the status check";

            foreach (var value in this.Values)
            {
                value.Name = ((PullRequestState)value.Value).ToString("g");
            }
        }
    }
}