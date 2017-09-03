using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.Auth
{
    public interface IManageUserPermissions
    {
        ImmutableList<System.Linq.IGrouping<string, string>> GetUsersAndRoles();

        void RecordUser(string username, Work.IUnitOfWork work);
        void AddUserRole(string username, string role, Work.IUnitOfWork work);
        void RemoveUserRole(string username, string role, Work.IUnitOfWork work);
    }
}
