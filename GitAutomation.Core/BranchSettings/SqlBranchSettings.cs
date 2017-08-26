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
            commandText: @"
SELECT COALESCE([UpstreamBranch].BranchName, [DownstreamBranch].BranchName) AS [BranchName],
		COALESCE([DownstreamBranch].RecreateFromUpstream, 0) AS [RecreateFromUpstream],
		COALESCE([DownstreamBranch].BranchType, 'Feature') AS [BranchType]
  FROM [UpstreamBranch]
  FULL OUTER JOIN [DownstreamBranch] ON [UpstreamBranch].BranchName = [DownstreamBranch].BranchName
");

        public static readonly CommandBuilder GetDownstreamBranchesCommand = new CommandBuilder(
            commandText: @"
SELECT [DownstreamBranch].BranchName AS [BranchName],
		[DownstreamBranch].RecreateFromUpstream AS [RecreateFromUpstream],
		[DownstreamBranch].BranchType AS [BranchType]
  FROM [UpstreamBranch]
  INNER JOIN [DownstreamBranch] ON [UpstreamBranch].DownstreamBranch = [DownstreamBranch].BranchName
  WHERE [UpstreamBranch].[BranchName]=@BranchName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetUpstreamBranchesCommand = new CommandBuilder(
            commandText: @"
SELECT COALESCE([UpstreamBranch].BranchName, [DownstreamBranch].BranchName) AS [BranchName],
		COALESCE([DownstreamBranch].RecreateFromUpstream, 0) AS [RecreateFromUpstream],
		COALESCE([DownstreamBranch].BranchType, 'Feature') AS [BranchType]
  FROM [UpstreamBranch]
  LEFT JOIN [DownstreamBranch] ON [UpstreamBranch].BranchName = [DownstreamBranch].BranchName
  WHERE [DownstreamBranch]=@BranchName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetAllDownstreamBranchesCommand = new CommandBuilder(
            commandText: @"
WITH RecursiveUpstream ( DownstreamBranch, BranchName )
AS (
	SELECT DownstreamBranch, BranchName FROM [UpstreamBranch] WHERE BranchName=@BranchName
UNION ALL
	SELECT [UpstreamBranch].DownstreamBranch, RecursiveUpstream.BranchName
	FROM [UpstreamBranch]
	INNER JOIN RecursiveUpstream ON RecursiveUpstream.DownstreamBranch = [UpstreamBranch].BranchName
)
SELECT COALESCE([UpstreamBranch].BranchName, [DownstreamBranch].BranchName) AS [BranchName],
		COALESCE([DownstreamBranch].RecreateFromUpstream, 0) AS [RecreateFromUpstream],
		COALESCE([DownstreamBranch].BranchType, 'Feature') AS [BranchType]
  FROM (SELECT [BranchName]
		  FROM RecursiveUpstream
		  GROUP BY DownstreamBranch, BranchName
		) AS [UpstreamBranch]
  FULL OUTER JOIN [DownstreamBranch] ON [UpstreamBranch].BranchName = [DownstreamBranch].BranchName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetAllUpstreamBranchesCommand = new CommandBuilder(
            commandText: @"
WITH RecursiveDownstream ( DownstreamBranch, BranchName )
AS (
	SELECT DownstreamBranch, BranchName FROM [UpstreamBranch] WHERE DownstreamBranch=@BranchName
UNION ALL
	SELECT RecursiveDownstream.DownstreamBranch, [UpstreamBranch].BranchName
	FROM [UpstreamBranch]
	INNER JOIN RecursiveDownstream ON [UpstreamBranch].DownstreamBranch = RecursiveDownstream.BranchName
)
SELECT COALESCE([UpstreamBranch].BranchName, [DownstreamBranch].BranchName) AS [BranchName],
		COALESCE([DownstreamBranch].RecreateFromUpstream, 0) AS [RecreateFromUpstream],
		COALESCE([DownstreamBranch].BranchType, 'Feature') AS [BranchType]
  FROM (SELECT [BranchName]
		  FROM RecursiveDownstream
		  GROUP BY DownstreamBranch, BranchName
		) AS [UpstreamBranch]
  FULL OUTER JOIN [DownstreamBranch] ON [UpstreamBranch].BranchName = [DownstreamBranch].BranchName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetAllUpstreamRemovableBranchesCommand = new CommandBuilder(
            commandText: @"
WITH RecursiveDownstream ( DownstreamBranch, BranchName )
AS (
	SELECT DownstreamBranch, BranchName FROM [UpstreamBranch] WHERE DownstreamBranch=@BranchName
UNION ALL
	SELECT RecursiveDownstream.DownstreamBranch, [UpstreamBranch].BranchName
	FROM [UpstreamBranch]
	INNER JOIN RecursiveDownstream ON [UpstreamBranch].DownstreamBranch = RecursiveDownstream.BranchName
)
SELECT RecursiveDownstream.[BranchName]
  FROM RecursiveDownstream
  LEFT JOIN DownstreamBranch ON RecursiveDownstream.BranchName=DownstreamBranch.BranchName
  WHERE COALESCE(DownstreamBranch.BranchType, 'Feature') != 'ServiceLine'
  GROUP BY DownstreamBranch, RecursiveDownstream.BranchName
  ORDER BY DownstreamBranch
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder AddBranchPropagationCommand = new CommandBuilder(
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

        public static readonly CommandBuilder RemoveBranchPropagationCommand = new CommandBuilder(
            commandText: @"
DELETE FROM [UpstreamBranch]
WHERE BranchName=@UpstreamBranch AND DownstreamBranch=@DownstreamBranch
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@UpstreamBranch", p => p.DbType = System.Data.DbType.AnsiString },
                { "@DownstreamBranch", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetBranchBasicDetialsCommand = new CommandBuilder(
            commandText: @"SELECT [BranchName], [RecreateFromUpstream], [BranchType]
  FROM [DownstreamBranch]
  WHERE [BranchName]=@BranchName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder UpdateBranchSettingCommand = new CommandBuilder(
            commandText: @"
MERGE INTO [DownstreamBranch] AS Downstream
USING (SELECT 
    @BranchName AS BranchName
    , @RecreateFromUpstream AS RecreateFromUpstream
    , @BranchType As BranchType
) AS NewDownstream
ON Downstream.BranchName = NewDownstream.BranchName
WHEN MATCHED THEN UPDATE SET 
    RecreateFromUpstream=NewDownstream.RecreateFromUpstream
    , BranchType=NewDownstream.BranchType
WHEN NOT MATCHED THEN INSERT (
    BranchName
    , RecreateFromUpstream
    , BranchType
) VALUES (
    NewDownstream.BranchName 
    , NewDownstream.RecreateFromUpstream
    , NewDownstream.BranchType
)
;
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
                { "@RecreateFromUpstream", p => p.DbType = System.Data.DbType.Int32 },
                { "@BranchType", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder DeleteBranchSettingsCommand = new CommandBuilder(
            commandText: @"
DELETE FROM [UpstreamBranch]
WHERE  [BranchName]=@BranchName OR [DownstreamBranch]=@BranchName

DELETE FROM [DownstreamBranch]
WHERE  [BranchName]=@BranchName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder ConsolidateServiceLineCommand = new CommandBuilder(
            commandText: @"
DECLARE @OldDownstream TABLE (DownstreamBranch VARCHAR(256) INDEX IX1 CLUSTERED);
DECLARE @RemainingDownstream TABLE (DownstreamBranch VARCHAR(256) INDEX IX1 CLUSTERED);

WITH RecursiveDownstream ( DownstreamBranch, BranchName )
AS (
	SELECT DownstreamBranch, BranchName FROM [UpstreamBranch] WHERE DownstreamBranch=@BranchName
UNION ALL
	SELECT RecursiveDownstream.DownstreamBranch, [UpstreamBranch].BranchName
	FROM [UpstreamBranch]
	INNER JOIN RecursiveDownstream ON [UpstreamBranch].DownstreamBranch = RecursiveDownstream.BranchName
)
INSERT INTO @OldDownstream
SELECT RecursiveDownstream.BranchName FROM RecursiveDownstream
LEFT JOIN DownstreamBranch ON RecursiveDownstream.BranchName=DownstreamBranch.BranchName
WHERE COALESCE(DownstreamBranch.BranchType, 'Feature') != 'ServiceLine'
GROUP BY RecursiveDownstream.BranchName

INSERT INTO @RemainingDownstream (DownstreamBranch)
SELECT UpstreamBranch.DownstreamBranch
FROM UpstreamBranch
INNER JOIN @OldDownstream as ShouldMatch ON UpstreamBranch.BranchName=ShouldMatch.DownstreamBranch
LEFT JOIN @OldDownstream as DoNotMatch ON UpstreamBranch.DownstreamBranch=DoNotMatch.DownstreamBranch
WHERE DoNotMatch.DownstreamBranch IS NULL AND UpstreamBranch.DownstreamBranch != @BranchName
GROUP BY UpstreamBranch.DownstreamBranch;

MERGE INTO [DownstreamBranch] AS Downstream
USING (SELECT @ServiceLineBranchName AS BranchName) AS NewDownstream
ON Downstream.BranchName = NewDownstream.BranchName
WHEN NOT MATCHED THEN INSERT (BranchName, RecreateFromUpstream, BranchType) VALUES (NewDownstream.BranchName, 0, 'ServiceLine');

DELETE FROM [UpstreamBranch]
WHERE BranchName IN (SELECT DownstreamBranch FROM @OldDownstream) OR DownstreamBranch IN (SELECT DownstreamBranch FROM @OldDownstream);

DELETE FROM [DownstreamBranch]
WHERE BranchName IN (SELECT DownstreamBranch FROM @OldDownstream) OR (BranchName = @BranchName AND BranchType != 'ServiceLine');

MERGE INTO [UpstreamBranch] AS T
USING @RemainingDownstream AS NewDownstream
ON T.DownstreamBranch = NewDownstream.DownstreamBranch AND T.BranchName=@ServiceLineBranchName
WHEN NOT MATCHED THEN INSERT (BranchName, DownstreamBranch) VALUES (@ServiceLineBranchName, NewDownstream.DownstreamBranch);

", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
                { "@ServiceLineBranchName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        #endregion

        private readonly IBranchSettingsNotifiers notifiers;
        private readonly IServiceProvider serviceProvider;

        public SqlBranchSettings(IBranchSettingsNotifiers notifiers, IServiceProvider serviceProvider)
        {
            this.notifiers = notifiers;
            this.serviceProvider = serviceProvider;
        }

        public IObservable<ImmutableList<BranchBasicDetails>> GetConfiguredBranches()
        {
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetConfiguredBranchesOnce));
        }

        private async Task<ImmutableList<BranchBasicDetails>> GetConfiguredBranchesOnce(DbConnection connection)
        { 
            using (var command = GetConfiguredBranchesCommand.BuildFrom(connection, ImmutableDictionary<string, object>.Empty))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var results = new List<BranchBasicDetails>();
                    while (await reader.ReadAsync())
                    {
                        results.Add(ReadBranchBasicDetails(reader));
                    }
                    return results.ToImmutableList();
                }
            }
        }

        public IObservable<BranchDetails> GetBranchDetails(string branchName)
        {
            // TODO - better notification
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(async connection =>
                {
                    var settings = await GetBranchDetailOnce(branchName)(connection);
                    return new BranchDetails
                    {
                        BranchName = branchName,
                        RecreateFromUpstream = settings.RecreateFromUpstream,
                        BranchType = settings.BranchType,
                        DirectDownstreamBranches = await GetDownstreamBranchesOnce(branchName)(connection),
                        DirectUpstreamBranches = await GetUpstreamBranchesOnce(branchName)(connection),
                        DownstreamBranches = await GetAllDownstreamBranchesOnce(branchName)(connection),
                        UpstreamBranches = await GetAllUpstreamBranchesOnce(branchName)(connection),
                    };
                }));
        }

        private Func<DbConnection, Task<BranchBasicDetails>> GetBranchDetailOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetBranchBasicDetialsCommand.BuildFrom(connection, new Dictionary<string, object> { { "@BranchName", branchName } }))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return ReadBranchBasicDetails(reader);
                        }
                        return new BranchBasicDetails
                        {
                            BranchName = branchName,
                            RecreateFromUpstream = false,
                            BranchType = BranchType.Feature,
                        };
                    }
                }
            };
        }

        private static BranchBasicDetails ReadBranchBasicDetails(System.Data.IDataRecord reader)
        {
            return new BranchBasicDetails
            {
                BranchName = reader["BranchName"] as string,
                RecreateFromUpstream = Convert.ToInt32(reader["RecreateFromUpstream"]) == 1,
                BranchType = Enum.TryParse<BranchType>(reader["BranchType"] as string, out var branchType)
                    ? branchType
                    : BranchType.Feature,
            };
        }

        public IObservable<ImmutableList<BranchBasicDetails>> GetDownstreamBranches(string branchName)
        {
            return notifiers.GetDownstreamBranchesChangedNotifier(upstreamBranch: branchName).StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetDownstreamBranchesOnce(branchName)));
        }

        private Func<DbConnection, Task<ImmutableList<BranchBasicDetails>>> GetDownstreamBranchesOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetDownstreamBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@BranchName", branchName } }))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<BranchBasicDetails>();
                        while (await reader.ReadAsync())
                        {
                            results.Add(ReadBranchBasicDetails(reader));
                        }
                        return results.ToImmutableList();
                    }
                }
            };
        }

        public IObservable<ImmutableList<BranchBasicDetails>> GetAllDownstreamBranches(string branchName)
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetAllDownstreamBranchesOnce(branchName)));
        }

        private Func<DbConnection, Task<ImmutableList<BranchBasicDetails>>> GetAllDownstreamBranchesOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetAllDownstreamBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@BranchName", branchName } }))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<BranchBasicDetails>();
                        while (await reader.ReadAsync())
                        {
                            results.Add(ReadBranchBasicDetails(reader));
                        }
                        return results.ToImmutableList();
                    }
                }
            };
        }

        public IObservable<ImmutableList<BranchBasicDetails>> GetUpstreamBranches(string branchName)
        {
            return notifiers.GetUpstreamBranchesChangedNotifier(downstreamBranch: branchName).StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetUpstreamBranchesOnce(branchName)));
        }

        private Func<DbConnection, Task<ImmutableList<BranchBasicDetails>>> GetUpstreamBranchesOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetUpstreamBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@BranchName", branchName } }))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<BranchBasicDetails>();
                        while (await reader.ReadAsync())
                        {
                            results.Add(ReadBranchBasicDetails(reader));
                        }
                        return results.ToImmutableList();
                    }
                }
            };
        }

        public IObservable<ImmutableList<BranchBasicDetails>> GetAllUpstreamBranches(string branchName)
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetAllUpstreamBranchesOnce(branchName)));
        }

        private Func<DbConnection, Task<ImmutableList<BranchBasicDetails>>> GetAllUpstreamBranchesOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetAllUpstreamBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@BranchName", branchName } }))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<BranchBasicDetails>();
                        while (await reader.ReadAsync())
                        {
                            results.Add(ReadBranchBasicDetails(reader));
                        }
                        return results.ToImmutableList();
                    }
                }
            };
        }

        public IObservable<ImmutableList<string>> GetAllUpstreamRemovableBranches(string branchName)
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetAllUpstreamRemovableBranchesOnce(branchName)));
        }

        private Func<DbConnection, Task<ImmutableList<string>>> GetAllUpstreamRemovableBranchesOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetAllUpstreamRemovableBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@BranchName", branchName } }))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<string>();
                        while (await reader.ReadAsync())
                        {
                            results.Add(Convert.ToString(reader["BranchName"]));
                        }
                        return results.ToImmutableList();
                    }
                }
            };
        }

        public void UpdateBranchSetting(string branchName, bool recreateFromUpstream, BranchType branchType, IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            work.Defer(async sp =>
            {
                using (var command = Transacted(sp, UpdateBranchSettingCommand, new Dictionary<string, object> {
                    { "@BranchName", branchName },
                    { "@RecreateFromUpstream", recreateFromUpstream ? 1 : 0 },
                    { "@BranchType", branchType.ToString("g") },
                }))
                {
                    await command.ExecuteNonQueryAsync();
                }
            });
            // TODO - onCommit, notify changes
        }

        public void AddBranchPropagation(string upstreamBranch, string downstreamBranch, IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            work.Defer(async sp =>
            {
                using (var command = Transacted(sp, AddBranchPropagationCommand, new Dictionary<string, object> {
                    { "@UpstreamBranch", upstreamBranch },
                    { "@DownstreamBranch", downstreamBranch },
                }))
                {
                    await command.ExecuteNonQueryAsync();
                }
            });
            // TODO - onCommit, notify changes
        }

        public void RemoveBranchPropagation(string upstreamBranch, string downstreamBranch, IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            work.Defer(async sp =>
            {
                using (var command = Transacted(sp, RemoveBranchPropagationCommand, new Dictionary<string, object> {
                    { "@UpstreamBranch", upstreamBranch },
                    { "@DownstreamBranch", downstreamBranch },
                }))
                {
                    await command.ExecuteNonQueryAsync();
                }
            });
            // TODO - onCommit, notify changes
        }

        public void ConsolidateServiceLine(string releaseCandidateBranch, string serviceLineBranch, Work.IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            work.Defer(async sp =>
            {
                using (var command = Transacted(sp, ConsolidateServiceLineCommand, new Dictionary<string, object> {
                    { "@BranchName", releaseCandidateBranch },
                    { "@ServiceLineBranchName", serviceLineBranch },
                }))
                {
                    await command.ExecuteNonQueryAsync();
                }
            });
        }

        public void DeleteBranchSettings(string deletingBranch, IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            work.Defer(async sp =>
            {
                using (var command = Transacted(sp, DeleteBranchSettingsCommand, new Dictionary<string, object> {
                    { "@BranchName", deletingBranch },
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

        private async Task<T> WithConnection<T>(Func<DbConnection, Task<T>> target)
        {
            using (var scope = serviceProvider.CreateScope())
            using (var connection = GetSqlConnection(scope.ServiceProvider))
            {
                await connection.OpenAsync();
                return await target(connection);
            }
        }
    }
}
