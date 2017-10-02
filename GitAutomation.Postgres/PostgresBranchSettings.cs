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
using GitAutomation.Postgres;
using System.Linq;
using GitAutomation.BranchSettings;
using Npgsql;

namespace GitAutomation.Postgres
{
    class PostgresBranchSettings : IBranchSettings
    {
        #region Getters

        public static readonly CommandBuilder GetConfiguredBranchesCommand = new CommandBuilder(
            commandText: @"
SELECT Branch.GroupName,
		COALESCE(DownstreamBranch.RecreateFromUpstream, '0') AS RecreateFromUpstream,
		COALESCE(DownstreamBranch.BranchType, 'Feature') AS BranchType
  FROM (
	SELECT GroupName FROM UpstreamBranch UNION SELECT GroupName FROM DownstreamBranch GROUP BY GroupName
) AS Branch
  LEFT JOIN DownstreamBranch ON Branch.GroupName = DownstreamBranch.GroupName
");

        public static readonly CommandBuilder GetDownstreamBranchesCommand = new CommandBuilder(
            commandText: @"
SELECT DownstreamBranch.GroupName AS GroupName,
		DownstreamBranch.RecreateFromUpstream AS RecreateFromUpstream,
		DownstreamBranch.BranchType AS BranchType
  FROM UpstreamBranch
  INNER JOIN DownstreamBranch ON UpstreamBranch.DownstreamBranch = DownstreamBranch.GroupName
  WHERE UpstreamBranch.GroupName=@GroupName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@GroupName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetUpstreamBranchesCommand = new CommandBuilder(
            commandText: @"
SELECT COALESCE(UpstreamBranch.GroupName, DownstreamBranch.GroupName) AS GroupName,
		COALESCE(DownstreamBranch.RecreateFromUpstream, '0') AS RecreateFromUpstream,
		COALESCE(DownstreamBranch.BranchType, 'Feature') AS BranchType
  FROM UpstreamBranch
  LEFT JOIN DownstreamBranch ON UpstreamBranch.GroupName = DownstreamBranch.GroupName
  WHERE DownstreamBranch=@GroupName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@GroupName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetAllDownstreamBranchesCommand = new CommandBuilder(
            commandText: @"
WITH RECURSIVE RecursiveDownstream ( DownstreamBranch, GroupName, Ordinal )
AS (
	SELECT GroupName AS DownstreamBranch, GroupName, 0 FROM DownstreamBranch
UNION
	SELECT GroupName AS DownstreamBranch, GroupName, 0 FROM UpstreamBranch
UNION ALL
	SELECT UpstreamBranch.DownstreamBranch, RecursiveDownstream.GroupName, RecursiveDownstream.Ordinal + 1
	FROM UpstreamBranch
	INNER JOIN RecursiveDownstream ON RecursiveDownstream.DownstreamBranch = UpstreamBranch.GroupName
)
SELECT COALESCE(UpstreamBranch.DownstreamBranch, DownstreamBranch.GroupName) AS GroupName,
		COALESCE(DownstreamBranch.RecreateFromUpstream, '0') AS RecreateFromUpstream,
		COALESCE(DownstreamBranch.BranchType, 'Feature') AS BranchType,
        Ordinal
  FROM (SELECT DownstreamBranch, MAX(Ordinal) AS Ordinal
		  FROM RecursiveDownstream
		  GROUP BY DownstreamBranch
		) AS UpstreamBranch
  LEFT JOIN DownstreamBranch ON UpstreamBranch.DownstreamBranch = DownstreamBranch.GroupName
  ORDER BY Ordinal, UpstreamBranch.DownstreamBranch
");

        public static readonly CommandBuilder GetAllDownstreamBranchesFromBranchCommand = new CommandBuilder(
            commandText: @"
WITH RECURSIVE RecursiveUpstream ( DownstreamBranch, GroupName, Ordinal )
AS (
	SELECT DownstreamBranch, GroupName, 1 FROM UpstreamBranch WHERE GroupName=@GroupName
UNION ALL
	SELECT UpstreamBranch.DownstreamBranch, RecursiveUpstream.GroupName, RecursiveUpstream.Ordinal + 1
	FROM UpstreamBranch
	INNER JOIN RecursiveUpstream ON RecursiveUpstream.DownstreamBranch = UpstreamBranch.GroupName
)
SELECT COALESCE(UpstreamBranch.DownstreamBranch, DownstreamBranch.GroupName) AS GroupName,
		COALESCE(DownstreamBranch.RecreateFromUpstream, '0') AS RecreateFromUpstream,
		COALESCE(DownstreamBranch.BranchType, 'Feature') AS BranchType
  FROM (SELECT DownstreamBranch, MIN(Ordinal) AS Ordinal
		  FROM RecursiveUpstream
		  GROUP BY DownstreamBranch, GroupName
		) AS UpstreamBranch
  LEFT JOIN DownstreamBranch ON UpstreamBranch.DownstreamBranch = DownstreamBranch.GroupName
  ORDER BY Ordinal, UpstreamBranch.DownstreamBranch
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@GroupName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetAllUpstreamBranchesCommand = new CommandBuilder(
            commandText: @"
WITH RECURSIVE RecursiveDownstream ( DownstreamBranch, GroupName, Ordinal )
AS (
	SELECT DownstreamBranch, GroupName, 1 FROM UpstreamBranch WHERE DownstreamBranch=@GroupName
UNION ALL
	SELECT RecursiveDownstream.DownstreamBranch, UpstreamBranch.GroupName, RecursiveDownstream.Ordinal + 1
	FROM UpstreamBranch
	INNER JOIN RecursiveDownstream ON UpstreamBranch.DownstreamBranch = RecursiveDownstream.GroupName
)
SELECT COALESCE(UpstreamBranch.GroupName, DownstreamBranch.GroupName) AS GroupName,
		COALESCE(DownstreamBranch.RecreateFromUpstream, '0') AS RecreateFromUpstream,
		COALESCE(DownstreamBranch.BranchType, 'Feature') AS BranchType
  FROM (SELECT GroupName, MAX(RecursiveDownstream.Ordinal) AS Ordinal
		  FROM RecursiveDownstream
		  GROUP BY DownstreamBranch, GroupName
		) AS UpstreamBranch
  LEFT JOIN DownstreamBranch ON UpstreamBranch.GroupName = DownstreamBranch.GroupName
  ORDER BY Ordinal DESC, UpstreamBranch.GroupName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@GroupName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetAllUpstreamRemovableBranchesCommand = new CommandBuilder(
            commandText: @"
WITH RECURSIVE RecursiveDownstream ( DownstreamBranch, GroupName )
AS (
	SELECT DownstreamBranch, GroupName FROM UpstreamBranch WHERE DownstreamBranch=@GroupName
UNION ALL
	SELECT RecursiveDownstream.DownstreamBranch, UpstreamBranch.GroupName
	FROM UpstreamBranch
	INNER JOIN RecursiveDownstream ON UpstreamBranch.DownstreamBranch = RecursiveDownstream.GroupName
)
SELECT RecursiveDownstream.GroupName
  FROM RecursiveDownstream
  LEFT JOIN DownstreamBranch ON RecursiveDownstream.GroupName=DownstreamBranch.GroupName
  WHERE COALESCE(DownstreamBranch.BranchType, 'Feature') != 'ServiceLine'
  GROUP BY DownstreamBranch, RecursiveDownstream.GroupName
  ORDER BY DownstreamBranch
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@GroupName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder AddBranchPropagationCommand = new CommandBuilder(
            commandText: @"
INSERT INTO DownstreamBranch  (GroupName)
VALUES (@DownstreamBranch)
ON CONFLICT DO NOTHING;

INSERT INTO UpstreamBranch (GroupName, DownstreamBranch)
VALUES (@UpstreamBranch, @DownstreamBranch)
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@UpstreamBranch", p => p.DbType = System.Data.DbType.AnsiString },
                { "@DownstreamBranch", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder RemoveBranchPropagationCommand = new CommandBuilder(
            commandText: @"
DELETE FROM UpstreamBranch
WHERE GroupName=@UpstreamBranch AND DownstreamBranch=@DownstreamBranch
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@UpstreamBranch", p => p.DbType = System.Data.DbType.AnsiString },
                { "@DownstreamBranch", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetBranchBasicDetialsCommand = new CommandBuilder(
            commandText: @"SELECT GroupName, RecreateFromUpstream, BranchType
  FROM DownstreamBranch
  WHERE GroupName=@GroupName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@GroupName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder UpdateBranchSettingCommand = new CommandBuilder(
            commandText: @"
INSERT INTO DownstreamBranch (
    GroupName
    , RecreateFromUpstream
    , BranchType
) VALUES (
    @GroupName
    , @RecreateFromUpstream
    , @BranchType
)
ON CONFLICT (GroupName) DO UPDATE SET
    RecreateFromUpstream=@RecreateFromUpstream
    , BranchType=@BranchType
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@GroupName", p => p.DbType = System.Data.DbType.AnsiString },
                { "@RecreateFromUpstream", p => (p as NpgsqlParameter).NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Bit },
                { "@BranchType", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder DeleteBranchSettingsCommand = new CommandBuilder(
            commandText: @"
DELETE FROM UpstreamBranch
WHERE  GroupName=@GroupName OR DownstreamBranch=@GroupName;

DELETE FROM DownstreamBranch
WHERE  GroupName=@GroupName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@GroupName", p => p.DbType = System.Data.DbType.AnsiString },
            });
        
        public static readonly CommandBuilder ConsolidateBranchCommand = new CommandBuilder(
            commandText: @"
INSERT INTO DownstreamBranch (GroupName, RecreateFromUpstream, BranchType) 
VALUES (@ReplacementGroupName, '0', 'ServiceLine')
ON CONFLICT (GroupName) DO NOTHING;

INSERT INTO UpstreamBranch (GroupName, DownstreamBranch)
SELECT @ReplacementGroupName, UpstreamBranch.DownstreamBranch
		FROM UpstreamBranch
		WHERE UpstreamBranch.GroupName = @GroupName
		GROUP BY UpstreamBranch.DownstreamBranch
ON CONFLICT (GroupName, DownstreamBranch) DO NOTHING;

DELETE FROM UpstreamBranch
WHERE GroupName = @GroupName OR DownstreamBranch=@GroupName;

DELETE FROM DownstreamBranch
WHERE GroupName = @GroupName
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@GroupName", p => p.DbType = System.Data.DbType.AnsiString },
                { "@ReplacementGroupName", p => p.DbType = System.Data.DbType.AnsiString },
            });

        public static readonly CommandBuilder GetIntegrationBranchCommand = new CommandBuilder(
            commandText: @"
SELECT 
	DownstreamBranch.GroupName,
	DownstreamBranch.RecreateFromUpstream,
	DownstreamBranch.BranchType,
	BranchA.GroupName As BranchA,
	BranchB.GroupName as BranchB,
	BranchC.GroupName as BranchC
	FROM DownstreamBranch
LEFT JOIN UpstreamBranch as BranchA ON BranchA.DownstreamBranch=DownstreamBranch.GroupName
LEFT JOIN UpstreamBranch as BranchB ON BranchB.DownstreamBranch=DownstreamBranch.GroupName AND BranchA.GroupName < BranchB.GroupName
LEFT JOIN UpstreamBranch as BranchC ON BranchC.DownstreamBranch=DownstreamBranch.GroupName AND BranchB.GroupName < BranchC.GroupName
WHERE BranchType='Integration' AND BranchA.GroupName=@BranchA AND BranchB.GroupName=@BranchB AND BranchC.GroupName IS NULL
", parameters: new Dictionary<string, Action<DbParameter>>
            {
                { "@BranchA", p => p.DbType = System.Data.DbType.AnsiString },
                { "@BranchB", p => p.DbType = System.Data.DbType.AnsiString },
            });

        #endregion

        private readonly IBranchSettingsNotifiers notifiers;
        private readonly IServiceProvider serviceProvider;

        public PostgresBranchSettings(IBranchSettingsNotifiers notifiers, IServiceProvider serviceProvider)
        {
            this.notifiers = notifiers;
            this.serviceProvider = serviceProvider;
        }

        public IObservable<ImmutableList<BranchGroupDetails>> GetConfiguredBranches()
        {
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetConfiguredBranchesOnce));
        }

        private async Task<ImmutableList<BranchGroupDetails>> GetConfiguredBranchesOnce(DbConnection connection)
        { 
            using (var command = GetConfiguredBranchesCommand.BuildFrom(connection, ImmutableDictionary<string, object>.Empty))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var results = new List<BranchGroupDetails>();
                    while (await reader.ReadAsync())
                    {
                        results.Add(ReadBranchBasicDetails(reader));
                    }
                    return results.ToImmutableList();
                }
            }
        }

        public IObservable<BranchGroupDetails> GetBranchBasicDetails(string branchName)
        {
            // TODO - better notification
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetBranchDetailOnce(branchName)));
        }

        public IObservable<BranchGroupCompleteData> GetBranchDetails(string branchName)
        {
            // TODO - better notification
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(async connection =>
                {
                    var settings = await GetBranchDetailOnce(branchName)(connection);
                    return new BranchGroupCompleteData
                    {
                        GroupName = branchName,
                        RecreateFromUpstream = settings.RecreateFromUpstream,
                        BranchType = settings.BranchType,
                        DirectDownstreamBranchGroups = (await GetDownstreamBranchesOnce(branchName)(connection)).Select(d => d.GroupName).ToImmutableList(),
                        DirectUpstreamBranchGroups = (await GetUpstreamBranchesOnce(branchName)(connection)).Select(d => d.GroupName).ToImmutableList(),
                        DownstreamBranchGroups = (await GetAllDownstreamBranchesOnce(branchName)(connection)).Select(d => d.GroupName).ToImmutableList(),
                        UpstreamBranchGroups = (await GetAllUpstreamBranchesOnce(branchName)(connection)).Select(d => d.GroupName).ToImmutableList(),
                    };
                }));
        }

        private Func<DbConnection, Task<BranchGroupDetails>> GetBranchDetailOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetBranchBasicDetialsCommand.BuildFrom(connection, new Dictionary<string, object> { { "@GroupName", branchName } }))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return ReadBranchBasicDetails(reader);
                        }
                        return new BranchGroupDetails
                        {
                            GroupName = branchName,
                            RecreateFromUpstream = false,
                            BranchType = BranchGroupType.Feature,
                        };
                    }
                }
            };
        }

        private static BranchGroupDetails ReadBranchBasicDetails(System.Data.IDataRecord reader)
        {
            return new BranchGroupDetails
            {
                GroupName = reader["GroupName"] as string,
                RecreateFromUpstream = (reader["RecreateFromUpstream"] as System.Collections.BitArray)?.Get(0) ?? (reader["RecreateFromUpstream"] as bool?) == true,
                BranchType = Enum.TryParse<BranchGroupType>(reader["BranchType"] as string, out var branchType)
                    ? branchType
                    : BranchGroupType.Feature,
            };
        }

        private static BranchGroupCompleteData ReadBranchDepthDetails(System.Data.IDataRecord reader)
        {
            return new BranchGroupCompleteData
            {
                GroupName = reader["GroupName"] as string,
                RecreateFromUpstream = (reader["RecreateFromUpstream"] as System.Collections.BitArray)[0],
                BranchType = Enum.TryParse<BranchGroupType>(reader["BranchType"] as string, out var branchType)
                    ? branchType
                    : BranchGroupType.Feature,
                HierarchyDepth = Convert.ToInt32(reader["Ordinal"])
            };
        }

        public IObservable<ImmutableList<BranchGroupDetails>> GetDownstreamBranches(string branchName)
        {
            return notifiers.GetDownstreamBranchesChangedNotifier(upstreamBranch: branchName).StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetDownstreamBranchesOnce(branchName)));
        }

        private Func<DbConnection, Task<ImmutableList<BranchGroupDetails>>> GetDownstreamBranchesOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetDownstreamBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@GroupName", branchName } }))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<BranchGroupDetails>();
                        while (await reader.ReadAsync())
                        {
                            results.Add(ReadBranchBasicDetails(reader));
                        }
                        return results.ToImmutableList();
                    }
                }
            };
        }

        public IObservable<ImmutableList<BranchGroupCompleteData>> GetAllDownstreamBranches()
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetAllDownstreamBranchesOnce()));
        }

        private Func<DbConnection, Task<ImmutableList<BranchGroupCompleteData>>> GetAllDownstreamBranchesOnce()
        {
            return async connection =>
            {
                using (var command = GetAllDownstreamBranchesCommand.BuildFrom(connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<BranchGroupCompleteData>();
                        while (await reader.ReadAsync())
                        {
                            results.Add(ReadBranchDepthDetails(reader));
                        }
                        return results.ToImmutableList();
                    }
                }
            };
        }

        public IObservable<ImmutableList<BranchGroupDetails>> GetAllDownstreamBranches(string branchName)
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetAllDownstreamBranchesOnce(branchName)));
        }

        private Func<DbConnection, Task<ImmutableList<BranchGroupDetails>>> GetAllDownstreamBranchesOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetAllDownstreamBranchesFromBranchCommand.BuildFrom(connection, new Dictionary<string, object> { { "@GroupName", branchName } }))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<BranchGroupDetails>();
                        while (await reader.ReadAsync())
                        {
                            results.Add(ReadBranchBasicDetails(reader));
                        }
                        return results.ToImmutableList();
                    }
                }
            };
        }

        public IObservable<ImmutableList<BranchGroupDetails>> GetUpstreamBranches(string branchName)
        {
            return notifiers.GetUpstreamBranchesChangedNotifier(downstreamBranch: branchName).StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetUpstreamBranchesOnce(branchName)));
        }

        private Func<DbConnection, Task<ImmutableList<BranchGroupDetails>>> GetUpstreamBranchesOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetUpstreamBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@GroupName", branchName } }))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<BranchGroupDetails>();
                        while (await reader.ReadAsync())
                        {
                            results.Add(ReadBranchBasicDetails(reader));
                        }
                        return results.ToImmutableList();
                    }
                }
            };
        }

        public IObservable<ImmutableList<BranchGroupDetails>> GetAllUpstreamBranches(string branchName)
        {
            // TODO - better notifications
            return notifiers.GetAnyNotification().StartWith(Unit.Default)
                .SelectMany(_ => WithConnection(GetAllUpstreamBranchesOnce(branchName)));
        }

        private Func<DbConnection, Task<ImmutableList<BranchGroupDetails>>> GetAllUpstreamBranchesOnce(string branchName)
        {
            return async connection =>
            {
                using (var command = GetAllUpstreamBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@GroupName", branchName } }))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<BranchGroupDetails>();
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
                using (var command = GetAllUpstreamRemovableBranchesCommand.BuildFrom(connection, new Dictionary<string, object> { { "@GroupName", branchName } }))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<string>();
                        while (await reader.ReadAsync())
                        {
                            results.Add(Convert.ToString(reader["GroupName"]));
                        }
                        return results.ToImmutableList();
                    }
                }
            };
        }

        public void UpdateBranchSetting(string branchName, bool recreateFromUpstream, BranchGroupType branchType, IUnitOfWork work)
        {
            PrepareSqlUnitOfWork(work);
            work.Defer(async sp =>
            {
                using (var command = GetConnectionManagement(sp).Transacted(UpdateBranchSettingCommand, new Dictionary<string, object> {
                    { "@GroupName", branchName },
                    { "@RecreateFromUpstream", recreateFromUpstream },
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
                    { "@GroupName", null },
                    { "@ReplacementGroupName", targetBranch },
                }))
                {
                    foreach (var branch in branchesToRemove)
                    {
                        command.Parameters["@GroupName"].Value = branch;
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
                    { "@GroupName", deletingBranch },
                }))
                {
                    await command.ExecuteNonQueryAsync();
                }
            });
        }

        public void CreateIntegrationBranch(string branchA, string branchB, string integrationGroupName, IUnitOfWork work)
        {
            UpdateBranchSetting(integrationGroupName, false, BranchGroupType.Integration, work);
            AddBranchPropagation(branchA, integrationGroupName, work);
            AddBranchPropagation(branchB, integrationGroupName, work);
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
