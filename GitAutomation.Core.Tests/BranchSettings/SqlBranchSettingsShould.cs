using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DeKreyConsulting.AdoTestability;

namespace GitAutomation.BranchSettings
{
    [TestClass]
    public class SqlBranchSettingsShould
    {
        private static SqlServerDriver database;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            database = new SqlServerDriver();
        }

        [TestMethod]
        public void HaveAValidGetConfiguredBranchesCommand()
        {
            SqlBranchSettings.GetConfiguredBranchesCommand.ExplainMultipleResult(database.BuildSqlConnection());

        }
    }
}
