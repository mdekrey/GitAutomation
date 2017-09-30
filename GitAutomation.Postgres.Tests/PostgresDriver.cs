using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;

namespace GitAutomation
{
    class PostgresDriver
    {
        private string connectionString;

        public PostgresDriver()
        {
            this.connectionString = ConfigurationSingleton.Configuration.GetSection("postgres").GetValue<string>("connectionString");
        }

        public NpgsqlConnection BuildSqlConnection() =>
            (NpgsqlConnection)NpgsqlFactory.Instance.CreateConnection(connectionString);
    }
}
