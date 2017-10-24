using GitAutomation.BranchSettings;
using GitAutomation.GraphQL.Resolvers;
using GitAutomation.Repository;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading.Tasks;
using static GitAutomation.GraphQL.Resolvers.Resolver;

namespace GitAutomation.GraphQL
{
    internal class GitAutomationQuery : ObjectGraphType<object>
    {
        public GitAutomationQuery()
        {
            Name = "Query";

            Field<BranchGroupDetailsInterface>()
                .Name("branchGroup")
                .Argument<NonNullGraphType<StringGraphType>>("name", "full name of the branch group")
                .Resolve(Resolve(this, nameof(BranchByName)));

            Field<ListGraphType<BranchGroupDetailsInterface>>()
                .Name("configuredBranchGroups")
                .Resolve(Resolve(this, nameof(BranchGroups)));

            Field<ListGraphType<GitRefInterface>>()
                .Name("allActualBranches")
                .Resolve(Resolve(this, nameof(AllGitRefs)));
        }

        async Task<string> BranchByName([FromArgument] string name, [FromServices] IAuthorizationService authorizationService, [FromServices] IHttpContextAccessor httpContext)
        {
            await Authorize(authorizationService, httpContext, Auth.PolicyNames.Read);
            return name;
        }

        async Task<ImmutableList<string>> BranchGroups([FromServices] Loaders loaders, [FromServices] IAuthorizationService authorizationService, [FromServices] IHttpContextAccessor httpContext)
        {
            await Authorize(authorizationService, httpContext, Auth.PolicyNames.Read);
            return await loaders.LoadBranchGroups().ConfigureAwait(false);
        }

        async Task<ImmutableList<GitRef>> AllGitRefs([FromServices] Loaders loaders, [FromServices] IAuthorizationService authorizationService, [FromServices] IHttpContextAccessor httpContext)
        {
            await Authorize(authorizationService, httpContext, Auth.PolicyNames.Read);
            return await loaders.LoadAllGitRefs().ConfigureAwait(false);
        }

        private static async Task Authorize(IAuthorizationService authorizationService, IHttpContextAccessor httpContext, string policyName)
        {
            var authorization = await authorizationService.AuthorizeAsync(httpContext.HttpContext.User, policyName).ConfigureAwait(false);
            if (!authorization.Succeeded)
            {
                throw new Exception(httpContext.HttpContext.User.Identity.IsAuthenticated
                    ? "Not authorized"
                    : "Not authenticated");
            }
        }

    }
}