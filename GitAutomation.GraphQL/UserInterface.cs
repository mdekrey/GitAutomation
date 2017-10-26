using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;
using static GitAutomation.GraphQL.Utilities.Resolvers.Resolver;
using GitAutomation.GraphQL.Utilities.Resolvers;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace GitAutomation.GraphQL
{
    class UserInterface : ObjectGraphType<string>
    {
        public UserInterface()
        {
            Field("username", d => d)
                .Description("The user's name.");

            Field<ListGraphType<NonNullGraphType<RoleInterface>>>()
                .Name("roles")
                .Resolve(Resolve(this, nameof(LoadRoles)));
                
        }

        Task<ImmutableList<string>> LoadRoles([Source] string username, [FromServices] Loaders loaders)
        {
            return loaders.LoadRoles(username);
        }
    }
}
