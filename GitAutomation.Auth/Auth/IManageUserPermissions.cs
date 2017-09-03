using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Auth
{
    public interface IManageUserPermissions
    {
        Task<ImmutableDictionary<string, ImmutableList<string>>> GetUsersAndRoles();

        void RecordUser(string username, Work.IUnitOfWork work);
        void AddUserRole(string username, string role, Work.IUnitOfWork work);
        void RemoveUserRole(string username, string role, Work.IUnitOfWork work);
    }
}
