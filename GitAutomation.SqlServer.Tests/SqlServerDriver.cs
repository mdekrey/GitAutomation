using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;

namespace GitAutomation
{
    class SqlServerDriver
    {
        private string connectionString;

        public SqlServerDriver()
        {
            this.connectionString = ConfigurationSingleton.Configuration.GetSection("sqlServer").GetValue<string>("connectionString");
        }

        public SqlConnection BuildSqlConnection() =>
            (SqlConnection)SqlClientFactory.Instance.CreateConnection(connectionString);
    }
}
