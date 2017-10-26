using GitAutomation.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GitAutomation.EFCore.SecurityModel
{
    class EfPermissionAccessor : IUserPermissionAccessor
    {
        private readonly SecurityContext context;

        public EfPermissionAccessor(SecurityContext context)
        {
            this.context = context;
        }

        private IQueryable<User> Users => context.User.AsNoTracking();
        private IQueryable<UserRole> Roles => context.UserRole.AsNoTracking();

        public Task<ImmutableDictionary<string, ImmutableList<string>>> GetRolesByUser(string[] usernames)
        {
            return (from user in Users
                    where usernames.Contains(user.ClaimName)
                    select user).Include(user => user.Roles).ToDictionaryAsync(b => b.ClaimName, b => b.Roles.Select(r => r.Permission).ToImmutableList())
                .ContinueWith(t => t.Result.ToImmutableDictionary());
        }

        public Task<ImmutableList<string>> GetUsers()
        {
            return (from user in Users
                    select user.ClaimName).ToArrayAsync()
                .ContinueWith(t => t.Result.ToImmutableList());
        }

        public Task<ImmutableList<string>> GetRoles()
        {
            return Task.FromResult(Enum.GetValues(typeof(Permission)).Cast<Permission>().Select(v => v.ToString("g").ToLower()).ToImmutableList());
        }


        public Task<ImmutableDictionary<string, ImmutableList<string>>> GetUsersByRole(string[] roles)
        {
            return (from user in Users
                    from role in user.Roles
                    where roles.Contains(role.Permission)
                    group role.ClaimName by role.Permission).ToDictionaryAsync(b => b.Key, b => b.ToImmutableList())
                .ContinueWith(t => t.Result.ToImmutableDictionary());
        }
    }
}
