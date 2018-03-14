using GitAutomation.Auth;
using GitAutomation.Work;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.EFCore.SecurityModel
{
    class EfPermissionManagement : IManageUserPermissions, IPrincipalValidation
    {
        private readonly IServiceProvider serviceProvider;
        private readonly EfPermissionManagementOptions options;

        public EfPermissionManagement(IServiceProvider serviceProvider, IOptions<EfPermissionManagementOptions> options)
        {
            this.serviceProvider = serviceProvider;
            this.options = options.Value;
        }

        public void AddUserRole(string username, string role, IUnitOfWork work)
        {
            PrepareSecurityContextUnitOfWork(work);
            work.Defer(async sp =>
            {
                var connection = GetContext(sp);

                await connection.User.AddIfNotExists(new User { ClaimName = username }, user => user.ClaimName == username);
                await connection.UserRole.AddIfNotExists(new UserRole { ClaimName = username, Permission = role }, userRole => userRole.ClaimName == username && userRole.Permission == role);
            });
        }

        public Task<ImmutableDictionary<string, ImmutableList<string>>> GetUsersAndRoles()
        {
            return WithContext(async context =>
            {
                return (await context.User
                    .Include(u => u.Roles)
                    .AsNoTracking()
                    .ToDictionaryAsync(user => user.ClaimName, user => user.Roles.Select(r => r.Permission).ToImmutableList())
                )
                .ToImmutableDictionary();
            });
        }

        public Task<ClaimsPrincipal> OnValidatePrincipal(HttpContext httpContext, ClaimsPrincipal currentPrincipal)
        {
            if (!currentPrincipal.Identity.IsAuthenticated)
            {
                return Task.FromResult(currentPrincipal);
            }

            // TODO - should cache this and clear it when an add/remove role instruction comes in for the given user
            return WithContext(async context =>
            {
                var claimsIdentity = currentPrincipal.Identity as ClaimsIdentity;
                var values = claimsIdentity.FindAll(options.ClaimType).Select(v => v.Value);
                await (from r in context.UserRole
                       where values.Contains(r.ClaimName)
                       select r.Permission)
                    .ForEachAsync(role => claimsIdentity.AddClaim(new Claim(Constants.PermissionType, role)));
                return currentPrincipal;
            });
        }

        public void RecordUser(string username, IUnitOfWork work)
        {
            PrepareSecurityContextUnitOfWork(work);
            work.Defer(async sp =>
            {
                var connection = GetContext(sp);

                await connection.User.AddIfNotExists(new User { ClaimName = username }, user => user.ClaimName == username);
            });
        }

        public void RemoveUserRole(string username, string role, IUnitOfWork work)
        {
            PrepareSecurityContextUnitOfWork(work);
            work.Defer(async sp =>
            {
                var connection = GetContext(sp);
                var userRole = await connection.UserRole.FirstOrDefaultAsync(ur => ur.ClaimName == username && ur.Permission == role);
                if (userRole != null)
                {
                    connection.UserRole.Remove(userRole);
                }
            });
        }


        private void PrepareSecurityContextUnitOfWork(IUnitOfWork work)
        {
            work.PrepareAndFinalize<ConnectionManagement<SecurityContext>>();
        }

        private SecurityContext GetContext(IServiceProvider scope) =>
            scope.GetRequiredService<SecurityContext>();

        private async Task<T> WithContext<T>(Func<SecurityContext, Task<T>> target)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                return await target(GetContext(scope.ServiceProvider));
            }
        }
    }
}
