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
        
    }
}
