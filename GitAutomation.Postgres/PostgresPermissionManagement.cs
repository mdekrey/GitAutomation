using GitAutomation.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using GitAutomation.Work;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using DeKreyConsulting.AdoTestability;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;

namespace GitAutomation.Postgres
{
    class PostgresPermissionManagement : IManageUserPermissions, IPrincipalValidation
    {
        #region Commands

        public static readonly CommandBuilder GetRolesForUserCommand = new CommandBuilder(
            commandText: @"
SELECT Role
  FROM UserRole
  WHERE ClaimName=@ClaimName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@ClaimName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetUsersAndRolesCommand = new CommandBuilder(
            commandText: @"
SELECT ClaimedUser.ClaimName
      ,Role
  FROM ClaimedUser
  LEFT JOIN UserRole ON ClaimedUser.ClaimName=UserRole.ClaimName
");

        public static readonly CommandBuilder EnsureUserCommand = new CommandBuilder(
            commandText: @"
INSERT INTO ClaimedUser (ClaimName) 
VALUES (@ClaimName)
ON CONFLICT (ClaimName) DO NOTHING
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@ClaimName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder AddRoleCommand = new CommandBuilder(
            commandText: @"
INSERT INTO UserRole (ClaimName, Role)
VALUES (@ClaimName, @Role)
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@ClaimName", p => p.DbType = System.Data.DbType.AnsiString },
                { "@Role", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder DeleteRoleCommand = new CommandBuilder(
            commandText: @"
DELETE FROM UserRole
WHERE ClaimName=@ClaimName AND Role=@Role
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@ClaimName", p => p.DbType = System.Data.DbType.AnsiString },
                { "@Role", p => p.DbType = System.Data.DbType.AnsiString },
            });

        #endregion

        private readonly IServiceProvider serviceProvider;

        public PostgresPermissionManagement(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void AddUserRole(string username, string role, IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            work.Defer(async sp =>
            {
                using (var command = GetConnectionManagement(sp).Transacted(EnsureUserCommand, new Dictionary<string, object> {
                    { "@ClaimName", username },
                }))
                {
                    await command.ExecuteNonQueryAsync();
                }
                using (var command = GetConnectionManagement(sp).Transacted(AddRoleCommand, new Dictionary<string, object> {
                    { "@ClaimName", username },
                    { "@Role", role },
                }))
                {
                    await command.ExecuteNonQueryAsync();
                }
            });
        }

        public Task<ImmutableDictionary<string, ImmutableList<string>>> GetUsersAndRoles()
        {
            return WithConnection(async connection =>
            {
                using (var command = GetUsersAndRolesCommand.BuildFrom(connection, ImmutableDictionary<string, object>.Empty))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var results = new List<Tuple<string, string>>();
                    while (await reader.ReadAsync())
                    {
                        results.Add(new Tuple<string, string>(reader["ClaimName"] as string, reader["Role"] as string));
                    }
                    return results.GroupBy(t => t.Item1, t => t.Item2)
                        .ToImmutableDictionary(e => e.Key, e => e.Where(v => v != null).ToImmutableList());
                }
            });
        }

        public Task<ClaimsPrincipal> OnValidatePrincipal(HttpContext httpContext, ClaimsPrincipal currentPrincipal)
        {
            if (!currentPrincipal.Identity.IsAuthenticated)
            {
                return Task.FromResult(currentPrincipal);
            }

            // TODO - should cache this and clear it when an add/remove role instruction comes in for the given user
            return WithConnection(async connection =>
            {
                using (var command = GetRolesForUserCommand.BuildFrom(connection, new Dictionary<string, object> { { "@ClaimName", currentPrincipal.Identity.Name } }))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var claimsIdentity = currentPrincipal.Identity as ClaimsIdentity;
                    while (await reader.ReadAsync())
                    {
                        claimsIdentity.AddClaim(new Claim(Auth.Constants.PermissionType, reader["Role"] as string));
                    }
                    return currentPrincipal;
                }
            });
        }

        public void RecordUser(string username, IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            work.Defer(async sp =>
            {
                using (var command = GetConnectionManagement(sp).Transacted(EnsureUserCommand, new Dictionary<string, object> {
                    { "@ClaimName", username },
                }))
                {
                    await command.ExecuteNonQueryAsync();
                }
            });
        }

        public void RemoveUserRole(string username, string role, IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            work.Defer(async sp =>
            {
                using (var command = GetConnectionManagement(sp).Transacted(DeleteRoleCommand, new Dictionary<string, object> {
                    { "@ClaimName", username },
                    { "@Role", role },
                }))
                {
                    await command.ExecuteNonQueryAsync();
                }
            });
        }


        private void PrepareSqlUnitOfWork(IUnitOfWork work)
        {
            work.PrepareAndFinalize<ConnectionManagement>();
        }

        private ConnectionManagement GetConnectionManagement(IServiceProvider scope) =>
            scope.GetRequiredService<ConnectionManagement>();

        private async Task<T> WithConnection<T>(Func<DbConnection, Task<T>> target)
        {
            using (var scope = serviceProvider.CreateScope())
            using (var connection = GetConnectionManagement(scope.ServiceProvider).Connection)
            {
                await connection.OpenAsync();
                return await target(connection);
            }
        }
    }
}
