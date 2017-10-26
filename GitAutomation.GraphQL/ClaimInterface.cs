using GraphQL.Types;

namespace GitAutomation.GraphQL
{
    internal class ClaimInterface : ObjectGraphType<System.Security.Claims.Claim>
    {
        public ClaimInterface()
        {
            Field(c => c.Type);
            Field(c => c.Value);
        }
    }
}