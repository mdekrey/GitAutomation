﻿using GitAutomation.BranchSettings;
using GitAutomation.GraphQL.Utilities.Resolvers;
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
using static GitAutomation.GraphQL.Utilities.Resolvers.Resolver;

namespace GitAutomation.GraphQL
{
    public class GitAutomationQuery : ObjectGraphType<object>
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

            Field<ListGraphType<NonNullGraphType<StringGraphType>>>()
                .Name("currentRoles")
                .Resolve(Resolve(this, nameof(CurrentRoles)));

            Field<ListGraphType<ClaimInterface>>()
                .Name("currentClaims")
                .Resolve(Resolve(this, nameof(CurrentClaims)));

            Field<ListGraphType<NonNullGraphType<UserInterface>>>()
                .Name("users")
                .Resolve(Resolve(this, nameof(GetUsers)));

            Field<NonNullGraphType<UserInterface>>()
                .Name("user")
                .Argument<NonNullGraphType<StringGraphType>>("username", "username for the user")
                .Resolve(Resolve(this, nameof(GetUser)));

            Field<ListGraphType<NonNullGraphType<RoleInterface>>>()
                .Name("roles")
                .Resolve(Resolve(this, nameof(GetRoles)));

            Field<NonNullGraphType<RoleInterface>>()
                .Name("role")
                .Argument<NonNullGraphType<StringGraphType>>("role", "role to retrieve")
                .Resolve(Resolve(this, nameof(GetRole)));
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