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
        class BranchBasicDetails
        {
            public bool RecreateFromUpstream { get; set; }
            public bool IsServiceLine { get; set; }
            public string ConflictResolutionMode { get; set; }
        }

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
SELECT [DownstreamBranch]
  FROM RecursiveUpstream
  GROUP BY DownstreamBranch, BranchName
  ORDER BY DownstreamBranch
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
SELECT [BranchName]
  FROM RecursiveDownstream
  GROUP BY DownstreamBranch, BranchName
  ORDER BY DownstreamBranch
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
  WHERE DownstreamBranch.IsServiceLine IS NULL OR DownstreamBranch.IsServiceLine = 0
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
            commandText: @"SELECT [RecreateFromUpstream], [IsServiceLine], [ConflictResolutionMode]
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
    , @IsServiceLine As IsServiceLine
    , @ConflictResolutionMode As ConflictResolutionMode
) AS NewDownstream
ON Downstream.BranchName = NewDownstream.BranchName
WHEN MATCHED THEN UPDATE SET 
    RecreateFromUpstream=NewDownstream.RecreateFromUpstream
    , IsServiceLine=NewDownstream.IsServiceLine
    , ConflictResolutionMode=NewDownstream.ConflictResolutionMode
WHEN NOT MATCHED THEN INSERT (
    BranchName
    , RecreateFromUpstream
    , IsServiceLine
    , ConflictResolutionMode
) VALUES (
    NewDownstream.BranchName 
    , NewDownstream.RecreateFromUpstream
    , NewDownstream.IsServiceLine
    , NewDownstream.ConflictResolutionMode
)
;
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
                { "@RecreateFromUpstream", p => p.DbType = System.Data.DbType.Int32 },
                { "@IsServiceLine", p => p.DbType = System.Data.DbType.Int32 },
                { "@ConflictResolutionMode", p => p.DbType = System.Data.DbType.AnsiString },
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
DECLARE @OldDownstream TABLE (DownstreamBranch VARCHAR(256));
DECLARE @RemainingDownstream TABLE (DownstreamBranch VARCHAR(256));

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
WHERE DownstreamBranch.IsServiceLine IS NULL OR DownstreamBranch.IsServiceLine = 0
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
WHEN NOT MATCHED THEN INSERT (BranchName, RecreateFromUpstream, IsServiceLine) VALUES (NewDownstream.BranchName, 0, 1);

DELETE FROM [UpstreamBranch]
WHERE BranchName IN (SELECT DownstreamBranch FROM @OldDownstream) OR DownstreamBranch IN (SELECT DownstreamBranch FROM @OldDownstream);

DELETE FROM [DownstreamBranch]
WHERE BranchName IN (SELECT DownstreamBranch FROM @OldDownstream) OR (BranchName = @BranchName AND IsServiceLine = 0);

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

        public IObservable<ImmutableList<string>> GetConfiguredBranches()
        {
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetConfiguredBranchesOnce));
        }

        private async Task<ImmutableList<string>> GetConfiguredBranchesOnce(DbConnection connection)
        { 
            using (var command = GetConfiguredBranchesCommand.BuildFrom(connection, ImmutableDictionary<string, object>.Empty))
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
                        IsServiceLine = settings.IsServiceLine,
                        ConflictResolutionMode = settings.ConflictResolutionMode,
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
                            return new BranchBasicDetails
                            {
                                RecreateFromUpstream = Convert.ToInt32(reader["RecreateFromUpstream"]) == 1,
                                IsServiceLine = Convert.ToInt32(reader["IsServiceLine"]) == 1,
                                ConflictResolutionMode = reader["ConflictResolutionMode"] as string,
                            };
                        }
                        return new BranchBasicDetails
                        {
                            RecreateFromUpstream = false,
                            IsServiceLine = false,
                            ConflictResolutionMode = "PullRequest",
                        };
                    }
                }
            };
        }

        public IObservable<ImmutableList<string>> GetDownstreamBranches(string branchName)
        {
            return notifiers.GetDownstreamBranchesChangedNotifier(upstreamBranch: branchName).StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetDownstreamBranchesOnce(branchName)));
        }

        private Func<DbConnection, Task<ImmutableList<string>>> GetDownstreamBranchesOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetDownstreamBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@BranchName", branchName } }))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<string>();
                        while (await reader.ReadAsync())
                        {
                            results.Add(Convert.ToString(reader["DownstreamBranch"]));
                        }
                        return results.ToImmutableList();
                    }
                }
            };
        }

        public IObservable<ImmutableList<string>> GetAllDownstreamBranches(string branchName)
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetAllDownstreamBranchesOnce(branchName)));
        }

        private Func<DbConnection, Task<ImmutableList<string>>> GetAllDownstreamBranchesOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetAllDownstreamBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@BranchName", branchName } }))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<string>();
                        while (await reader.ReadAsync())
                        {
                            results.Add(Convert.ToString(reader["DownstreamBranch"]));
                        }
                        return results.ToImmutableList();
                    }
                }
            };
        }

        public IObservable<ImmutableList<string>> GetUpstreamBranches(string branchName)
        {
            return notifiers.GetUpstreamBranchesChangedNotifier(downstreamBranch: branchName).StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetUpstreamBranchesOnce(branchName)));
        }

        private Func<DbConnection, Task<ImmutableList<string>>> GetUpstreamBranchesOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetUpstreamBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@BranchName", branchName } }))
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

        public IObservable<ImmutableList<string>> GetAllUpstreamBranches(string branchName)
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetAllUpstreamBranchesOnce(branchName)));
        }

        private Func<DbConnection, Task<ImmutableList<string>>> GetAllUpstreamBranchesOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetAllUpstreamBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@BranchName", branchName } }))
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

        public void UpdateBranchSetting(string branchName, bool recreateFromUpstream, bool isServiceLine, string conflictResolutionMode, IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            work.Defer(async sp =>
            {
                using (var command = Transacted(sp, UpdateBranchSettingCommand, new Dictionary<string, object> {
                    { "@BranchName", branchName },
                    { "@RecreateFromUpstream", recreateFromUpstream ? 1 : 0 },
                    { "@IsServiceLine", isServiceLine ? 1 : 0 },
                    { "@ConflictResolutionMode", conflictResolutionMode },
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
