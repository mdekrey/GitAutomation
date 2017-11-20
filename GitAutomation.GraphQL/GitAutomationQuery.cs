using GitAutomation.BranchSettings;
using GitAutomation.GraphQL.Utilities.Resolvers;
using GitAutomation.Orchestration;
using GitAutomation.Processes;
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

namespace GitAutomation.GraphQL
{
    public class GitAutomationQuery : ObjectGraphType<object>
    {
        public GitAutomationQuery()
        {
            Name = "Query";

            Field<NonNullGraphType<AppOptionsInterface>>()
                .Name("app")
                .Resolve(this, nameof(App));

            Field<NonNullGraphType<ListGraphType<OrchestrationActionInterface>>>()
                .Name("orchestrationQueue")
                .Resolve(this, nameof(OrchestrationQueue));

            Field<NonNullGraphType<ListGraphType<RepositoryActionEntryInterface>>>()
                .Name("log")
                .Resolve(this, nameof(OrchestrationLog));

            Field<NonNullGraphType<BranchGroupDetailsInterface>>()
                .Name("branchGroup")
                .Argument<NonNullGraphType<StringGraphType>>("name", "full name of the branch group")
                .Resolve(this, nameof(BranchByName));

            Field<NonNullGraphType<ListGraphType<BranchGroupDetailsInterface>>>()
                .Name("configuredBranchGroups")
                .Resolve(this, nameof(BranchGroups));

            Field<NonNullGraphType<ListGraphType<GitRefInterface>>>()
                .Name("allActualBranches")
                .Resolve(this, nameof(AllGitRefs));

            Field<ListGraphType<NonNullGraphType<StringGraphType>>>()
                .Name("currentRoles")
                .Resolve(this, nameof(CurrentRoles));

            Field<NonNullGraphType<ListGraphType<ClaimInterface>>>()
                .Name("currentClaims")
                .Resolve(this, nameof(CurrentClaims));

            Field<ListGraphType<NonNullGraphType<UserInterface>>>()
                .Name("users")
                .Resolve(this, nameof(GetUsers));

            Field<NonNullGraphType<UserInterface>>()
                .Name("user")
                .Argument<NonNullGraphType<StringGraphType>>("username", "username for the user")
                .Resolve(this, nameof(GetUser));

            Field<ListGraphType<NonNullGraphType<RoleInterface>>>()
                .Name("roles")
                .Resolve(this, nameof(GetRoles));

            Field<NonNullGraphType<RoleInterface>>()
                .Name("role")
                .Argument<NonNullGraphType<StringGraphType>>("role", "role to retrieve")
                .Resolve(this, nameof(GetRole));
        }

        Task<AppOptions> App([FromServices] AppOptions app)
        {
            return Task.FromResult(app);
        }

        async Task<ImmutableList<IRepositoryAction>> OrchestrationQueue([FromServices] IRepositoryOrchestration orchestration, [FromServices] IAuthorizationService authorizationService, [FromServices] IHttpContextAccessor httpContext)
        {
            await Authorize(authorizationService, httpContext, Auth.PolicyNames.Read);
            return await orchestration.ActionQueue.FirstAsync();
        }

        async Task<ImmutableList<IRepositoryActionEntry>> OrchestrationLog([FromServices] IRepositoryOrchestration orchestration, [FromServices] IAuthorizationService authorizationService, [FromServices] IHttpContextAccessor httpContext)
        {
            await Authorize(authorizationService, httpContext, Auth.PolicyNames.Read);
            return await orchestration.ProcessActionsLog.FirstAsync();
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

        Task<ImmutableList<string>> CurrentRoles([FromServices] IHttpContextAccessor httpContext)
        {
            return Task.FromResult(
                (from claim in httpContext.HttpContext.User.Claims
                 where claim.Type == Auth.Constants.PermissionType
                 select claim.Value).ToImmutableList()
            );
        }

        Task<ImmutableList<System.Security.Claims.Claim>> CurrentClaims([FromServices] IHttpContextAccessor httpContext)
        {
            return Task.FromResult(httpContext.HttpContext.User.Claims.ToImmutableList());
        }

        async Task<ImmutableList<string>> GetUsers([FromServices] Loaders loaders, [FromServices] IAuthorizationService authorizationService, [FromServices] IHttpContextAccessor httpContext)
        {
            await Authorize(authorizationService, httpContext, Auth.PolicyNames.Administrate).ConfigureAwait(false);
            return await loaders.GetUsers().ConfigureAwait(false);
        }

        async Task<string> GetUser([FromArgument] string username, [FromServices] IAuthorizationService authorizationService, [FromServices] IHttpContextAccessor httpContext)
        {
            await Authorize(authorizationService, httpContext, Auth.PolicyNames.Administrate).ConfigureAwait(false);
            return username;
        }

        async Task<ImmutableList<string>> GetRoles([FromServices] Loaders loaders, [FromServices] IAuthorizationService authorizationService, [FromServices] IHttpContextAccessor httpContext)
        {
            await Authorize(authorizationService, httpContext, Auth.PolicyNames.Administrate).ConfigureAwait(false);
            return await loaders.GetRoles().ConfigureAwait(false);
        }

        async Task<string> GetRole([FromArgument] string role, [FromServices] IAuthorizationService authorizationService, [FromServices] IHttpContextAccessor httpContext)
        {
            await Authorize(authorizationService, httpContext, Auth.PolicyNames.Administrate).ConfigureAwait(false);
            return role;
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