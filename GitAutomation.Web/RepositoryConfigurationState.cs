using GitAutomation.DomainModels;
using System;
using System.Collections.Immutable;

namespace GitAutomation.Web
{
    public class RepositoryConfigurationState
    {
        public RepositoryConfigurationState(
            DateTimeOffset storedFieldModifiedTimestamp,
            DateTimeOffset loadedFromDiskTimestamp,
            DateTimeOffset needPullTimestamp,
            DateTimeOffset pulledTimestamp,
            DateTimeOffset pushedTimestamp,
            RepositoryConfigurationLastError lastError,
            RepositoryConfiguration configuration,
            RepositoryStructure structure)
        {
            StoredFieldModifiedTimestamp = storedFieldModifiedTimestamp;
            LoadedFromDiskTimestamp = loadedFromDiskTimestamp;
            NeedPullTimestamp = needPullTimestamp;
            PulledTimestamp = pulledTimestamp;
            PushedTimestamp = pushedTimestamp;
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
        }

        public DateTimeOffset StoredFieldModifiedTimestamp { get; }
        public DateTimeOffset LoadedFromDiskTimestamp { get; }
        public DateTimeOffset NeedPullTimestamp { get; }
        public DateTimeOffset PulledTimestamp { get; }
        public DateTimeOffset PushedTimestamp { get; }
        public RepositoryConfigurationLastError LastError { get; }
        public RepositoryConfiguration Configuration { get; }
        public RepositoryStructure Structure { get; }

        public static RepositoryConfigurationState ZeroState { get; } = new RepositoryConfigurationState(
            storedFieldModifiedTimestamp: DateTimeOffset.MinValue,
            loadedFromDiskTimestamp: DateTimeOffset.MinValue,
            needPullTimestamp: DateTimeOffset.Now,
            pulledTimestamp: DateTimeOffset.MinValue,
            pushedTimestamp: DateTimeOffset.MinValue,
            lastError: RepositoryConfigurationLastError.None,
            configuration: new RepositoryConfiguration(),
            structure: new RepositoryStructure(ImmutableSortedDictionary<string, BranchReserve>.Empty));

        public RepositoryConfigurationState With(
            DateTimeOffset? storedFieldModifiedTimestamp = null,
            DateTimeOffset? loadedFromDiskTimestamp = null,
            DateTimeOffset? needPullTimestamp = null,
            DateTimeOffset? pulledTimestamp = null,
            DateTimeOffset? pushedTimestamp = null,
            RepositoryConfigurationLastError? lastError = null,
            RepositoryConfiguration configuration = null,
            RepositoryStructure structure = null)
        {
            if ((storedFieldModifiedTimestamp ?? StoredFieldModifiedTimestamp) != StoredFieldModifiedTimestamp
                || (loadedFromDiskTimestamp ?? LoadedFromDiskTimestamp) != LoadedFromDiskTimestamp
                || (needPullTimestamp ?? NeedPullTimestamp) != NeedPullTimestamp
                || (pulledTimestamp ?? PulledTimestamp) != PulledTimestamp
                || (pushedTimestamp ?? PushedTimestamp) != PushedTimestamp
                || (lastError ?? LastError) != LastError
                || (configuration ?? Configuration) != Configuration
                || (structure ?? Structure) != Structure)
            {
                return new RepositoryConfigurationState(
                    storedFieldModifiedTimestamp: storedFieldModifiedTimestamp ?? StoredFieldModifiedTimestamp,
                    loadedFromDiskTimestamp: loadedFromDiskTimestamp ?? LoadedFromDiskTimestamp,
                    needPullTimestamp: needPullTimestamp ?? NeedPullTimestamp,
                    pulledTimestamp: pulledTimestamp ?? PulledTimestamp,
                    pushedTimestamp: pushedTimestamp ?? PushedTimestamp,
                    lastError: lastError ?? LastError,
                    configuration: configuration ?? Configuration,
                    structure: structure ?? Structure);
            }
            return this;
        }
    }
}