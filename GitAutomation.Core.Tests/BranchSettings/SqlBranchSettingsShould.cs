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
        public void HaveAValidGetConfiguredBranchesCommand() =>
            SqlBranchSettings.GetConfiguredBranchesCommand.ExplainMultipleResult(database.BuildSqlConnection());


        [TestMethod]
        public void HaveAValidGetDownstreamBranchesCommand() =>
            SqlBranchSettings.GetDownstreamBranchesCommand.ExplainMultipleResult(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidGetUpstreamBranchesCommand() =>
            SqlBranchSettings.GetUpstreamBranchesCommand.ExplainMultipleResult(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidGetAllDownstreamBranchesCommand() =>
            SqlBranchSettings.GetAllDownstreamBranchesCommand.ExplainMultipleResult(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidGetAllUpstreamBranchesCommand() =>
            SqlBranchSettings.GetAllUpstreamBranchesCommand.ExplainMultipleResult(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidGetAllUpstreamRemovableBranchesCommand() =>
            SqlBranchSettings.GetAllUpstreamRemovableBranchesCommand.ExplainMultipleResult(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidAddBranchPropagationCommand() =>
            SqlBranchSettings.AddBranchPropagationCommand.ExplainMultipleResult(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidRemoveBranchPropagationCommand() =>
            SqlBranchSettings.RemoveBranchPropagationCommand.ExplainMultipleResult(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidGetBranchBasicDetialsCommand() =>
            SqlBranchSettings.GetBranchBasicDetialsCommand.ExplainMultipleResult(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidUpdateBranchSettingCommand() =>
            SqlBranchSettings.UpdateBranchSettingCommand.ExplainMultipleResult(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidDeleteBranchSettingsCommand() =>
            SqlBranchSettings.DeleteBranchSettingsCommand.ExplainMultipleResult(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidConsolidateServiceLineCommand() =>
            SqlBranchSettings.ConsolidateServiceLineCommand.ExplainMultipleResult(database.BuildSqlConnection());
    }
}
