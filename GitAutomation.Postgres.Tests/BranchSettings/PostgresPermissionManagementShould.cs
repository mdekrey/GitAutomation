using DeKreyConsulting.AdoTestability;
using GitAutomation.Postgres;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.BranchSettings
{
    [TestClass]
    public class PostgresPermissionManagementShould
    {
        private static PostgresDriver database;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            database = new PostgresDriver();
        }

        [TestMethod]
        public void HaveAValidGetRolesForUserCommand() =>
            PostgresPermissionManagement.GetRolesForUserCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidGetUsersAndRolesCommand() =>
            PostgresPermissionManagement.GetUsersAndRolesCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidEnsureUserCommand() =>
            PostgresPermissionManagement.EnsureUserCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidAddRoleCommand() =>
            PostgresPermissionManagement.AddRoleCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidDeleteRoleCommand() =>
            PostgresPermissionManagement.DeleteRoleCommand.ValidateCommand(database.BuildSqlConnection());

    }
}
