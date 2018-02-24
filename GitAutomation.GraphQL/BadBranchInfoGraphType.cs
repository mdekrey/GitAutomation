using GitAutomation.Repository;
using GraphQL.Types;

namespace GitAutomation.GraphQL
{
    internal class BadBranchInfoGraphType : ObjectGraphType<BadBranchInfo>
    {
        public BadBranchInfoGraphType()
        {
            Field(r => r.ReasonCode);
            Field(r => r.Timestamp, type: typeof(DateGraphType));
        }
    }
}