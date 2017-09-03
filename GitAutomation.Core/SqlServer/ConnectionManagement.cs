using GitAutomation.Work;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.SqlServer
{
    class ConnectionManagement : IUnitOfWorkLifecycleManagement
    {
        private readonly SqlConnection connection;
        private SqlTransaction transaction;

        public ConnectionManagement(string connectionString)
        {
            this.connection = new SqlConnection(connectionString);
        }

        public DbConnection Connection => connection;
        public SqlTransaction Transaction => transaction;

        Task IUnitOfWorkLifecycleManagement.Commit()
        {
            transaction.Commit();
            return Task.CompletedTask;
        }

        async Task IUnitOfWorkLifecycleManagement.Prepare()
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
            transaction = connection.BeginTransaction();
        }

        Task IUnitOfWorkLifecycleManagement.Rollback()
        {
            transaction.Rollback();
            transaction.Dispose();
            return Task.CompletedTask;
        }
    }
}
