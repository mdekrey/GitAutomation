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

namespace GitAutomation.BranchSettings
{
    class SqlBranchSettings : IBranchSettings
    {

        #region Getters

        public static readonly CommandBuilder GetConfiguredBranchesCommand = new CommandBuilder(
            commandText: @"SELECT [BranchName]
  FROM [GitAutomation].[dbo].[UpstreamBranch]
UNION
SELECT [BranchName]
  FROM [GitAutomation].[dbo].[DownstreamBranch]
");

        public static readonly CommandBuilder GetDownstreamBranchesCommand = new CommandBuilder(
            commandText: @"SELECT [DownstreamBranch]
  FROM [GitAutomation].[dbo].[UpstreamBranch]
  WHERE [BranchName]=@BranchName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetUpstreamBranchesCommand = new CommandBuilder(
            commandText: @"SELECT [BranchName]
  FROM [GitAutomation].[dbo].[UpstreamBranch]
  WHERE [DownstreamBranch]=@BranchName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
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
            using (var connection = GetSqlConnection(scope))
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
            using (var connection = GetSqlConnection(scope))
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
            using (var connection = GetSqlConnection(scope))
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
            // TODO
        }

        public void RemoveBranchSetting(string upstreamBranch, string downstreamBranch, IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            // TODO
        }

        private void PrepareSqlUnitOfWork(IUnitOfWork work)
        {
            throw new NotImplementedException();
        }

        private DbConnection GetSqlConnection(IServiceScope scope)
        {
            return scope.ServiceProvider.GetRequiredService<DbConnection>();
        }

    }
}
