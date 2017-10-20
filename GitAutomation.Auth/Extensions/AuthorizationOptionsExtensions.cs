using GitAutomation.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Authorization
{
    public static class AuthorizationOptionsExtensions
    {
        public static void AddGitAutomationPolicies(this AuthorizationOptions options)
        {
            var anyPermission = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.DefaultPolicy = anyPermission;


            options.AddPolicy(
                PolicyNames.Read,
                new AuthorizationPolicyBuilder()
                    .Combine(anyPermission)
                    .RequireRole(Permission.Administrate.ToString().ToLower(), Permission.Read.ToString().ToLower())
                    .Build()
            );
            options.AddPolicy(
                PolicyNames.Create,
                new AuthorizationPolicyBuilder()
                    .Combine(anyPermission)
                    .RequireRole(Permission.Administrate.ToString().ToLower(), Permission.Create.ToString().ToLower())
                    .Build()
            );
            options.AddPolicy(
                PolicyNames.Delete,
                new AuthorizationPolicyBuilder()
                    .Combine(anyPermission)
                    .RequireRole(Permission.Administrate.ToString().ToLower(), Permission.Delete.ToString().ToLower())
                    .Build()
            );
            options.AddPolicy(
                PolicyNames.Update,
                new AuthorizationPolicyBuilder()
                    .Combine(anyPermission)
                    .RequireRole(Permission.Administrate.ToString().ToLower(), Permission.Update.ToString().ToLower())
                    .Build()
            );
            options.AddPolicy(
                PolicyNames.Approve,
                new AuthorizationPolicyBuilder()
                    .Combine(anyPermission)
                    .RequireRole(Permission.Administrate.ToString().ToLower(), Permission.Approve.ToString().ToLower())
                    .Build()
            );
            options.AddPolicy(
                PolicyNames.Administrate,
                new AuthorizationPolicyBuilder()
                    .Combine(anyPermission)
                    .RequireRole(Permission.Administrate.ToString().ToLower())
                    .Build()
            );

        }
    }
}
