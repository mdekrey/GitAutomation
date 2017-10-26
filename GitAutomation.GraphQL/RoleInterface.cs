using GraphQL.Types;
using GitAutomation.GraphQL.Utilities.Resolvers;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace GitAutomation.GraphQL
{
    internal class RoleInterface : ObjectGraphType<string>
    {
        public RoleInterface()
        {
            Field("role", d => d)
                .Description("The role's name.");

            Field<ListGraphType<NonNullGraphType<RoleInterface>>>()
                .Name("users")
                .Resolve(this, nameof(LoadUsers));

        }

        Task<ImmutableList<string>> LoadUsers([Source] string role, [FromServices] Loaders loaders)
        {
            return loaders.LoadUsers(role);
        }
    }
}