using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace GitAutomation.DomainModels
{
    public class RepositoryConfigurationState
    {
        public enum ConfigurationTimestampType
        {
            StoredFieldModified,
            LoadedFromDisk,
            NeedPull,
            Pulled,
            Pushed,
        }

        public RepositoryConfigurationState(
            ImmutableSortedDictionary<ConfigurationTimestampType, DateTimeOffset> timestamps,
            RepositoryConfigurationLastError lastError,
            RepositoryConfiguration configuration,
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
        public RepositoryConfiguration Configuration { get; }
        public RepositoryStructure Structure { get; }

        public static RepositoryConfigurationState ZeroState { get; } = new RepositoryConfigurationState(
            timestamps: ImmutableSortedDictionary.CreateRange(new Dictionary<ConfigurationTimestampType, DateTimeOffset>
            {
                { ConfigurationTimestampType.StoredFieldModified, DateTimeOffset.MinValue },
                { ConfigurationTimestampType.LoadedFromDisk, DateTimeOffset.MinValue },
                { ConfigurationTimestampType.NeedPull, DateTimeOffset.Now },
                { ConfigurationTimestampType.Pulled, DateTimeOffset.MinValue },
                { ConfigurationTimestampType.Pushed, DateTimeOffset.MinValue },
            }),
            lastError: RepositoryConfigurationLastError.None,
            configuration: new RepositoryConfiguration(),
            structure: new RepositoryStructure(ImmutableSortedDictionary<string, BranchReserve>.Empty));

        public RepositoryConfigurationState With(
            Func<ImmutableSortedDictionary<ConfigurationTimestampType, DateTimeOffset>, ImmutableSortedDictionary<ConfigurationTimestampType, DateTimeOffset>>? timestampFunc = null,
            RepositoryConfigurationLastError? lastError = null,
            RepositoryConfiguration? configuration = null,
            RepositoryStructure? structure = null)
        {
            var timestamps = timestampFunc?.Invoke(Timestamps) ?? Timestamps;
            if ((timestamps ?? Timestamps) != Timestamps
                || (lastError ?? LastError) != LastError
                || (configuration ?? Configuration) != Configuration
                || (structure ?? Structure) != Structure)
            {
                return new RepositoryConfigurationState(
                    timestamps: timestamps ?? Timestamps,
                    lastError: lastError ?? LastError,
                    configuration: configuration ?? Configuration,
                    structure: structure ?? Structure);
            }
            return this;
        }
    }
}