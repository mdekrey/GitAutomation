using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Auth
{
    public interface IUserPermissionAccessor
    {
        Task<ImmutableList<string>> GetUsers();
        Task<ImmutableDictionary<string, ImmutableList<string>>> GetRolesByUser(string[] usernames);
        Task<ImmutableList<string>> GetRoles();
        Task<ImmutableDictionary<string, ImmutableList<string>>> GetUsersByRole(string[] roles);

    }
}
