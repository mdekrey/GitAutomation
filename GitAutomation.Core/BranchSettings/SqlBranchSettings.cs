using System;
using System.Collections.Generic;
using System.Text;
using GitAutomation.Work;
using DeKreyConsulting.AdoTestability;
using System.Data.Common;
using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive;
using GitAutomation.SqlServer;

namespace GitAutomation.BranchSettings
{
    class SqlBranchSettings : IBranchSettings
    {

        #region Getters

        public static readonly CommandBuilder GetConfiguredBranchesCommand = new CommandBuilder(
            commandText: @"SELECT [BranchName]
  FROM [UpstreamBranch]
UNION
SELECT [BranchName]
  FROM [DownstreamBranch]
");

        public static readonly CommandBuilder GetDownstreamBranchesCommand = new CommandBuilder(
            commandText: @"SELECT [DownstreamBranch]
  FROM [UpstreamBranch]
  WHERE [BranchName]=@BranchName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetUpstreamBranchesCommand = new CommandBuilder(
            commandText: @"SELECT [BranchName]
  FROM [UpstreamBranch]
  WHERE [DownstreamBranch]=@BranchName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder AddBranchSettingCommand = new CommandBuilder(
            commandText: @"
MERGE INTO [DownstreamBranch] AS Downstream
USING (SELECT @DownstreamBranch AS BranchName) AS NewDownstream
ON Downstream.BranchName = NewDownstream.BranchName
WHEN NOT MATCHED THEN INSERT (BranchName) VALUES (NewDownstream.BranchName);

INSERT INTO [UpstreamBranch] (BranchName, DownstreamBranch)
VALUES (@UpstreamBranch, @DownstreamBranch)
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@UpstreamBranch", p => p.DbType = System.Data.DbType.AnsiString },
                { "@DownstreamBranch", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder RemoveBranchSettingCommand = new CommandBuilder(
            commandText: @"
DELETE FROM [UpstreamBranch]
WHERE BranchName=@UpstreamBranch AND DownstreamBranch=@DownstreamBranch)
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@UpstreamBranch", p => p.DbType = System.Data.DbType.AnsiString },
                { "@DownstreamBranch", p => p.DbType = System.Data.DbType.AnsiString },
            });

        #endregion

        private readonly IBranchSettingsNotifiers notifiers;
        private readonly IServiceProvider serviceProvider;

        public SqlBranchSettings(IBranchSettingsNotifiers notifiers, IServiceProvider serviceProvider)
        {
            this.notifiers = notifiers;
            this.serviceProvider = serviceProvider;
        }

        public IObservable<string[]> GetConfiguredBranches()
        {
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => GetConfiguredBranchesOnce());
        }

        private async Task<string[]> GetConfiguredBranchesOnce()
        { 
            using (var scope = serviceProvider.CreateScope())
            using (var connection = GetSqlConnection(scope.ServiceProvider))
            using (var command = GetConfiguredBranchesCommand.BuildFrom(connection, ImmutableDictionary<string, object>.Empty))
            {
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var results = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        results.Add(Convert.ToString(reader["BranchName"]));
                    }
                    return results.ToArray();
                }
            }
        }

        public IObservable<string[]> GetDownstreamBranches(string branchName)
        {
            return notifiers.GetDownstreamBranchesChangedNotifier(upstreamBranch: branchName).StartWith(Unit.Default)
                .SelectMany(_ => GetDownstreamBranchesOnce(branchName));
        }

        private async Task<string[]> GetDownstreamBranchesOnce(string branchName)
        {
            using (var scope = serviceProvider.CreateScope())
            using (var connection = GetSqlConnection(scope.ServiceProvider))
            using (var command = GetDownstreamBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@BranchName", branchName } }))
            {
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var results = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        results.Add(Convert.ToString(reader["DownstreamBranch"]));
                    }
                    return results.ToArray();
                }
            }
        }

        public IObservable<string[]> GetUpstreamBranches(string branchName)
        {
            return notifiers.GetUpstreamBranchesChangedNotifier(downstreamBranch: branchName).StartWith(Unit.Default)
                .SelectMany(_ => GetUpstreamBranchesOnce(branchName));
        }

        private async Task<string[]> GetUpstreamBranchesOnce(string branchName)
        {
            using (var scope = serviceProvider.CreateScope())
            using (var connection = GetSqlConnection(scope.ServiceProvider))
            using (var command = GetUpstreamBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@BranchName", branchName } }))
            {
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var results = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        results.Add(Convert.ToString(reader["BranchName"]));
                    }
                    return results.ToArray();
                }
            }
        }

        public void AddBranchSetting(string upstreamBranch, string downstreamBranch, IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            work.Defer(async sp =>
            {
                using (var command = Transacted(sp, AddBranchSettingCommand, new Dictionary<string, object> {
                    { "@UpstreamBranch", upstreamBranch },
                    { "@DownstreamBranch", downstreamBranch },
                }))
                {
                    await command.ExecuteNonQueryAsync();
                }
            });
        }

        public void RemoveBranchSetting(string upstreamBranch, string downstreamBranch, IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            work.Defer(async sp =>
            {
                using (var command = Transacted(sp, RemoveBranchSettingCommand, new Dictionary<string, object> {
                    { "@UpstreamBranch", upstreamBranch },
                    { "@DownstreamBranch", downstreamBranch },
                }))
                {
                    await command.ExecuteNonQueryAsync();
                }
            });
        }

        private void PrepareSqlUnitOfWork(IUnitOfWork work)
        {
            work.PrepareAndFinalize<ConnectionManagement>();
        }

        private DbCommand Transacted(IServiceProvider sp, CommandBuilder commandBuilder, Dictionary<string, object> parameters)
        {
            return commandBuilder.BuildFrom(GetSqlConnection(sp), parameters, GetSqlTransaction(sp));
        }

        private DbConnection GetSqlConnection(IServiceProvider scope)
        {
            return scope.GetRequiredService<ConnectionManagement>().Connection;
        }

        private DbTransaction GetSqlTransaction(IServiceProvider scope)
        {
            return scope.GetRequiredService<ConnectionManagement>().Transaction;
        }

    }
}
