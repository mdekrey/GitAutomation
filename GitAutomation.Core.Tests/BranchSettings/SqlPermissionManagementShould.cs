using DeKreyConsulting.AdoTestability;
using GitAutomation.SqlServer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.BranchSettings
{
    [TestClass]
    public class SqlPermissionManagementShould
    {
        private static SqlServerDriver database;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            database = new SqlServerDriver();
        }

        [TestMethod]
        public void HaveAValidGetRolesForUserCommand() =>
            SqlPermissionManagement.GetRolesForUserCommand.ExplainMultipleResult(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidGetUsersAndRolesCommand() =>
            SqlPermissionManagement.GetUsersAndRolesCommand.ExplainMultipleResult(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidEnsureUserCommand() =>
            SqlPermissionManagement.EnsureUserCommand.ExplainMultipleResult(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidAddRoleCommand() =>
            SqlPermissionManagement.AddRoleCommand.ExplainMultipleResult(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidDeleteRoleCommand() =>
            SqlPermissionManagement.DeleteRoleCommand.ExplainMultipleResult(database.BuildSqlConnection());

    }
}
