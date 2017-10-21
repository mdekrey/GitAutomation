using GraphQL.Types;

namespace GitAutomation.GraphQL
{
    internal class BranchGroupDetailsInterface : ObjectGraphType<BranchSettings.BranchGroupDetails>
    {
        public BranchGroupDetailsInterface()
        {
            Name = "BranchGroupDetails";

            Field(d => d.GroupName).Description("The full name of the group.");

            Field(d => d.RecreateFromUpstream).Description("Whether a new branch is recreated from upstream branches when new changes are about to be merged.");
            Field(d => d.BranchType, type: typeof(BranchGroupTypeEnum)).Description("The type of the branch.");
        }
    }
}