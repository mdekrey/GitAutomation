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
using System.Linq;
using GitAutomation.BranchSettings;

namespace GitAutomation.SqlServer
{
    class SqlBranchSettings : IBranchSettings
    {
        #region Getters

        public static readonly CommandBuilder GetConfiguredBranchesCommand = new CommandBuilder(
            commandText: @"
SELECT [Branch].BranchName,
		COALESCE([DownstreamBranch].RecreateFromUpstream, 0) AS [RecreateFromUpstream],
		COALESCE([DownstreamBranch].BranchType, 'Feature') AS [BranchType]
  FROM (
	SELECT BranchName FROM [UpstreamBranch] UNION SELECT BranchName FROM [DownstreamBranch] GROUP BY BranchName
) AS [Branch]
  LEFT JOIN [DownstreamBranch] ON [Branch].BranchName = [DownstreamBranch].BranchName
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
WITH RecursiveDownstream ( DownstreamBranch, BranchName, Ordinal )
AS (
	SELECT BranchName AS DownstreamBranch, BranchName, 0 FROM [DownstreamBranch]
UNION
	SELECT BranchName AS DownstreamBranch, BranchName, 0 FROM [UpstreamBranch]
UNION ALL
	SELECT [UpstreamBranch].DownstreamBranch, RecursiveDownstream.BranchName, RecursiveDownstream.Ordinal + 1
	FROM [UpstreamBranch]
	INNER JOIN RecursiveDownstream ON RecursiveDownstream.DownstreamBranch = [UpstreamBranch].BranchName
)
SELECT COALESCE([UpstreamBranch].DownstreamBranch, [DownstreamBranch].BranchName) AS [BranchName],
		COALESCE([DownstreamBranch].RecreateFromUpstream, 0) AS [RecreateFromUpstream],
		COALESCE([DownstreamBranch].BranchType, 'Feature') AS [BranchType],
        Ordinal
  FROM (SELECT DownstreamBranch, MAX(Ordinal) AS Ordinal
		  FROM RecursiveDownstream
		  GROUP BY DownstreamBranch
		) AS [UpstreamBranch]
  LEFT JOIN [DownstreamBranch] ON [UpstreamBranch].DownstreamBranch = [DownstreamBranch].BranchName
  ORDER BY Ordinal, [UpstreamBranch].DownstreamBranch
");

        public static readonly CommandBuilder GetAllDownstreamBranchesFromBranchCommand = new CommandBuilder(
            commandText: @"
WITH RecursiveUpstream ( DownstreamBranch, BranchName, Ordinal )
AS (
	SELECT DownstreamBranch, BranchName, 1 FROM [UpstreamBranch] WHERE BranchName=@BranchName
UNION ALL
	SELECT [UpstreamBranch].DownstreamBranch, RecursiveUpstream.BranchName, RecursiveUpstream.Ordinal + 1
	FROM [UpstreamBranch]
	INNER JOIN RecursiveUpstream ON RecursiveUpstream.DownstreamBranch = [UpstreamBranch].BranchName
)
SELECT COALESCE([UpstreamBranch].DownstreamBranch, [DownstreamBranch].BranchName) AS [BranchName],
		COALESCE([DownstreamBranch].RecreateFromUpstream, 0) AS [RecreateFromUpstream],
		COALESCE([DownstreamBranch].BranchType, 'Feature') AS [BranchType]
  FROM (SELECT DownstreamBranch, MIN(Ordinal) AS Ordinal
		  FROM RecursiveUpstream
		  GROUP BY DownstreamBranch, BranchName
		) AS [UpstreamBranch]
  LEFT JOIN [DownstreamBranch] ON [UpstreamBranch].DownstreamBranch = [DownstreamBranch].BranchName
  ORDER BY Ordinal, [UpstreamBranch].DownstreamBranch
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetAllUpstreamBranchesCommand = new CommandBuilder(
            commandText: @"
WITH RecursiveDownstream ( DownstreamBranch, BranchName, Ordinal )
AS (
	SELECT DownstreamBranch, BranchName, 1 FROM [UpstreamBranch] WHERE DownstreamBranch=@BranchName
UNION ALL
	SELECT RecursiveDownstream.DownstreamBranch, [UpstreamBranch].BranchName, RecursiveDownstream.Ordinal + 1
	FROM [UpstreamBranch]
	INNER JOIN RecursiveDownstream ON [UpstreamBranch].DownstreamBranch = RecursiveDownstream.BranchName
)
SELECT COALESCE([UpstreamBranch].BranchName, [DownstreamBranch].BranchName) AS [BranchName],
		COALESCE([DownstreamBranch].RecreateFromUpstream, 0) AS [RecreateFromUpstream],
		COALESCE([DownstreamBranch].BranchType, 'Feature') AS [BranchType]
  FROM (SELECT [BranchName], MAX(RecursiveDownstream.Ordinal) AS Ordinal
		  FROM RecursiveDownstream
		  GROUP BY DownstreamBranch, BranchName
		) AS [UpstreamBranch]
  LEFT JOIN [DownstreamBranch] ON [UpstreamBranch].BranchName = [DownstreamBranch].BranchName
  ORDER BY Ordinal DESC, [UpstreamBranch].BranchName
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
        
        public static readonly CommandBuilder ConsolidateBranchCommand = new CommandBuilder(
            commandText: @"
DECLARE @ChangedDownstream TABLE (DownstreamBranch VARCHAR(256) INDEX IX1 CLUSTERED);

INSERT INTO @ChangedDownstream (DownstreamBranch)
SELECT UpstreamBranch.DownstreamBranch
FROM UpstreamBranch
WHERE UpstreamBranch.BranchName = @BranchName
GROUP BY UpstreamBranch.DownstreamBranch;

MERGE INTO [DownstreamBranch] AS Downstream
USING (SELECT @ReplacementBranchName AS BranchName) AS NewDownstream
ON Downstream.BranchName = NewDownstream.BranchName
WHEN NOT MATCHED THEN INSERT (BranchName, RecreateFromUpstream, BranchType) VALUES (NewDownstream.BranchName, 0, 'ServiceLine');

DELETE FROM [UpstreamBranch]
WHERE BranchName = @BranchName;

DELETE FROM [DownstreamBranch]
WHERE BranchName = @BranchName;

MERGE INTO [UpstreamBranch] AS T
USING @ChangedDownstream AS NewDownstream
ON T.DownstreamBranch = NewDownstream.DownstreamBranch AND T.BranchName=@ReplacementBranchName
WHEN NOT MATCHED THEN INSERT (BranchName, DownstreamBranch) VALUES (@ReplacementBranchName, NewDownstream.DownstreamBranch);

", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchName", p => p.DbType = System.Data.DbType.AnsiString },
                { "@ReplacementBranchName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetIntegrationBranchCommand = new CommandBuilder(
            commandText: @"
SELECT 
	DownstreamBranch.BranchName,
	DownstreamBranch.RecreateFromUpstream,
	DownstreamBranch.BranchType,
	BranchA.BranchName As BranchA,
	BranchB.BranchName as BranchB,
	BranchC.BranchName as BranchC
	FROM [dbo].[DownstreamBranch]
LEFT JOIN [dbo].[UpstreamBranch] as BranchA ON BranchA.DownstreamBranch=DownstreamBranch.BranchName
LEFT JOIN [dbo].[UpstreamBranch] as BranchB ON BranchB.DownstreamBranch=DownstreamBranch.BranchName AND BranchA.BranchName < BranchB.BranchName
LEFT JOIN [dbo].[UpstreamBranch] as BranchC ON BranchC.DownstreamBranch=DownstreamBranch.BranchName AND BranchB.BranchName < BranchC.BranchName
WHERE BranchType='Integration' AND BranchA.BranchName=@BranchA AND BranchB.BranchName=@BranchB AND BranchC.BranchName IS NULL
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchA", p => p.DbType = System.Data.DbType.AnsiString },
                { "@BranchB", p => p.DbType = System.Data.DbType.AnsiString },
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

        private static BranchDepthDetails ReadBranchDepthDetails(System.Data.IDataRecord reader)
        {
            return new BranchDepthDetails
            {
                BranchName = reader["BranchName"] as string,
                RecreateFromUpstream = Convert.ToInt32(reader["RecreateFromUpstream"]) == 1,
                BranchType = Enum.TryParse<BranchType>(reader["BranchType"] as string, out var branchType)
                    ? branchType
                    : BranchType.Feature,
                Ordinal = Convert.ToInt32(reader["Ordinal"])
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

        public IObservable<ImmutableList<BranchDepthDetails>> GetAllDownstreamBranches()
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetAllDownstreamBranchesOnce()));
        }

        private Func<DbConnection, Task<ImmutableList<BranchDepthDetails>>> GetAllDownstreamBranchesOnce()
        {
            return async connection =>
            {
                using (var command = GetAllDownstreamBranchesCommand.BuildFrom(connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<BranchDepthDetails>();
                        while (await reader.ReadAsync())
                        {
                            results.Add(ReadBranchDepthDetails(reader));
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
                using (var command = GetAllDownstreamBranchesFromBranchCommand.BuildFrom(connection, new Dictionary<string, object> { { "@BranchName", branchName } }))
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

        public Task<string> GetIntegrationBranch(string branchA, string branchB)
        {
            var branches = new[] { branchA, branchB }.OrderBy(a => a).ToArray();
            return WithConnection(async connection =>
            {
                using (var command = GetIntegrationBranchCommand.BuildFrom(connection, new Dictionary<string, object>
                {
                    { "@BranchA", branches[0] },
                    { "@BranchB", branches[1] }
                }))
                {
                    return await command.ExecuteScalarAsync() as string;
                }
            });
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
                using (var command = GetConnectionManagement(sp).Transacted(UpdateBranchSettingCommand, new Dictionary<string, object> {
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
                using (var command = GetConnectionManagement(sp).Transacted(AddBranchPropagationCommand, new Dictionary<string, object> {
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
                using (var command = GetConnectionManagement(sp).Transacted(RemoveBranchPropagationCommand, new Dictionary<string, object> {
                    { "@UpstreamBranch", upstreamBranch },
                    { "@DownstreamBranch", downstreamBranch },
                }))
                {
                    await command.ExecuteNonQueryAsync();
                }
            });
            // TODO - onCommit, notify changes
        }

        public void ConsolidateBranches(IEnumerable<string> branchesToRemove, string targetBranch, IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            work.Defer(async sp =>
            {
                using (var command = GetConnectionManagement(sp).Transacted(ConsolidateBranchCommand, new Dictionary<string, object> {
                    { "@BranchName", null },
                    { "@ReplacementBranchName", targetBranch },
                }))
                {
                    foreach (var branch in branchesToRemove)
                    {
                        command.Parameters["@BranchName"].Value = branch;
                        await command.ExecuteNonQueryAsync();
                    }
                }
            });
        }

        public void DeleteBranchSettings(string deletingBranch, IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            work.Defer(async sp =>
            {
                using (var command = GetConnectionManagement(sp).Transacted(DeleteBranchSettingsCommand, new Dictionary<string, object> {
                    { "@BranchName", deletingBranch },
                }))
                {
                    await command.ExecuteNonQueryAsync();
                }
            });
        }

        public void CreateIntegrationBranch(string branchA, string branchB, string integrationBranchName, IUnitOfWork work)
        {
            UpdateBranchSetting(integrationBranchName, false, BranchType.Integration, work);
            AddBranchPropagation(branchA, integrationBranchName, work);
            AddBranchPropagation(branchB, integrationBranchName, work);
        }

        private void PrepareSqlUnitOfWork(IUnitOfWork work)
        {
            work.PrepareAndFinalize<ConnectionManagement>();
        }

        private ConnectionManagement GetConnectionManagement(IServiceProvider scope) =>
            scope.GetRequiredService<ConnectionManagement>();
        
        private async Task<T> WithConnection<T>(Func<DbConnection, Task<T>> target)
        {
            using (var scope = serviceProvider.CreateScope())
            using (var connection = GetConnectionManagement(scope.ServiceProvider).Connection)
            {
                await connection.OpenAsync();
                return await target(connection);
            }
        }
    }
}
