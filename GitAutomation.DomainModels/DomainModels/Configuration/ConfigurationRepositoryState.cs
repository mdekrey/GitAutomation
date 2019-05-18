using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace GitAutomation.DomainModels.Configuration
{
    public class ConfigurationRepositoryState
    {
        public enum ConfigurationTimestampType
        {
            StoredFieldModified,
            LoadedFromDisk,
            NeedPull,
            Pulled,
            Pushed,
        }

        public ConfigurationRepositoryState(
            ImmutableSortedDictionary<ConfigurationTimestampType, DateTimeOffset> timestamps,
            RepositoryConfigurationLastError lastError,
            ConfigurationRepository configuration,
            RepositoryStructure structure)
        {
            Timestamps = timestamps;
            LastError = lastError;
            Configuration = configuration;
            Structure = structure;
        }

        public enum RepositoryConfigurationLastError
        {
            None,
            Error_DirectoryNotAccessible,
            Error_FailedToClone,
            Error_PasswordIncorrect,
            Error_FailedToPush,
            Error_FailedToCommit,
            Error_NestedGitRepository,
        }

        public ImmutableSortedDictionary<ConfigurationTimestampType, DateTimeOffset> Timestamps { get; }
        public RepositoryConfigurationLastError LastError { get; }
        public ConfigurationRepository Configuration { get; }
        public RepositoryStructure Structure { get; }

        public static ConfigurationRepositoryState ZeroState { get; } = new ConfigurationRepositoryState(
            timestamps: ImmutableSortedDictionary.CreateRange(new Dictionary<ConfigurationTimestampType, DateTimeOffset>
            {
                { ConfigurationTimestampType.StoredFieldModified, DateTimeOffset.MinValue },
                { ConfigurationTimestampType.LoadedFromDisk, DateTimeOffset.MinValue },
                { ConfigurationTimestampType.NeedPull, DateTimeOffset.Now },
                { ConfigurationTimestampType.Pulled, DateTimeOffset.MinValue },
                { ConfigurationTimestampType.Pushed, DateTimeOffset.MinValue },
            }),
            lastError: RepositoryConfigurationLastError.None,
            configuration: new ConfigurationRepository(),
            structure: new RepositoryStructure(ImmutableSortedDictionary<string, BranchReserve>.Empty));

        public ConfigurationRepositoryState With(
            Func<ImmutableSortedDictionary<ConfigurationTimestampType, DateTimeOffset>, ImmutableSortedDictionary<ConfigurationTimestampType, DateTimeOffset>>? timestampFunc = null,
            RepositoryConfigurationLastError? lastError = null,
            ConfigurationRepository? configuration = null,
            RepositoryStructure? structure = null)
        {
            var timestamps = timestampFunc?.Invoke(Timestamps) ?? Timestamps;
            if ((timestamps ?? Timestamps) != Timestamps
                || (lastError ?? LastError) != LastError
                || (configuration ?? Configuration) != Configuration
                || (structure ?? Structure) != Structure)
            {
                return new ConfigurationRepositoryState(
                    timestamps: timestamps ?? Timestamps,
                    lastError: lastError ?? LastError,
                    configuration: configuration ?? Configuration,
                    structure: structure ?? Structure);
            }
            return this;
        }
    }
}