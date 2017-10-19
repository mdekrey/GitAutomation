using GitAutomation.BranchSettings;
using GraphQL.Types;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace GitAutomation.GraphQL
{
    internal class GitAutomationQuery : ObjectGraphType<object>
    {
        public GitAutomationQuery(IBranchSettings settings)
        {
            Name = "Query";

            FieldAsync<BranchGroupDetailsInterface>(
                "branchGroup",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "name", Description = "full name of the branch group" }
                ),
                resolve: async context =>
                {
                    var name = context.GetArgument<string>("name");
                    var branchGroup = await settings.GetBranchBasicDetails(name).FirstOrDefaultAsync();
                    return branchGroup;
                }
            );
        }

    }
}