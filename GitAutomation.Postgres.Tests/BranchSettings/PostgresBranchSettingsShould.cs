using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DeKreyConsulting.AdoTestability;
using GitAutomation.Postgres;

namespace GitAutomation.BranchSettings
{
    [TestClass]
    public class PostgresBranchSettingsShould
    {
        private static PostgresDriver database;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            database = new PostgresDriver();
        }

        [TestMethod]
        public void HaveAValidGetConfiguredBranchesCommand() =>
            PostgresBranchSettings.GetConfiguredBranchesCommand.ValidateCommand(database.BuildSqlConnection());


        [TestMethod]
        public void HaveAValidGetDownstreamBranchesCommand() =>
            PostgresBranchSettings.GetDownstreamBranchesCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidGetUpstreamBranchesCommand() =>
            PostgresBranchSettings.GetUpstreamBranchesCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidGetAllDownstreamBranchesCommand() =>
            PostgresBranchSettings.GetAllDownstreamBranchesCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidGetAllDownstreamBranchesFromBranchCommand() =>
            PostgresBranchSettings.GetAllDownstreamBranchesFromBranchCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidGetAllUpstreamBranchesCommand() =>
            PostgresBranchSettings.GetAllUpstreamBranchesCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidGetAllUpstreamRemovableBranchesCommand() =>
            PostgresBranchSettings.GetAllUpstreamRemovableBranchesCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidAddBranchPropagationCommand() =>
            PostgresBranchSettings.AddBranchPropagationCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidRemoveBranchPropagationCommand() =>
            PostgresBranchSettings.RemoveBranchPropagationCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidGetBranchBasicDetialsCommand() =>
            PostgresBranchSettings.GetBranchBasicDetialsCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidUpdateBranchSettingCommand() =>
            PostgresBranchSettings.UpdateBranchSettingCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidDeleteBranchSettingsCommand() =>
            PostgresBranchSettings.DeleteBranchSettingsCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidConsolidateBranchCommand() =>
            PostgresBranchSettings.ConsolidateBranchCommand.ValidateCommand(database.BuildSqlConnection());

        [TestMethod]
        public void HaveAValidGetIntegrationBranchCommand() =>
            PostgresBranchSettings.GetIntegrationBranchCommand.ValidateCommand(database.BuildSqlConnection());

    }
}
