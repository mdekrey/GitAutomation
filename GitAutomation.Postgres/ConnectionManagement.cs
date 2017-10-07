using GitAutomation.Work;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using DeKreyConsulting.AdoTestability;
using Npgsql;

namespace GitAutomation.Postgres
{
    class ConnectionManagement : IUnitOfWorkLifecycleManagement, IDisposable
    {
        private readonly NpgsqlConnection connection;
        private NpgsqlTransaction transaction;

        public ConnectionManagement(IOptions<PostgresOptions> options)
        {
            this.connection = new NpgsqlConnection(options.Value.ConnectionString);
        }

        public DbConnection Connection => connection;
        public NpgsqlTransaction Transaction => transaction;

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

        internal DbCommand Transacted(CommandBuilder commandBuilder, Dictionary<string, object> parameters)
        {
            return commandBuilder.BuildFrom(Connection, parameters, Transaction);
        }

        void IDisposable.Dispose()
        {
            connection?.Dispose();
            transaction?.Dispose();
        }
    }
}
