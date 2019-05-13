using GitAutomation.DomainModels;
using System.Collections.Immutable;

namespace GitAutomation.Web
{
    public class RepositoryConfigurationState
    {
        public RepositoryConfigurationState(
            bool isCurrentWithDisk,
            bool isPulled,
            bool isPushed,
            RepositoryConfigurationLastError lastError,
            RepositoryConfiguration configuration,
            RepositoryStructure structure)
        {
            IsCurrentWithDisk = isCurrentWithDisk;
            IsPulled = isPulled;
            IsPushed = isPushed;
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

        public bool IsCurrentWithDisk { get; }
        public bool IsPulled { get; }
        public bool IsPushed { get; }
        public RepositoryConfigurationLastError LastError { get; }
        public RepositoryConfiguration Configuration { get; }
        public RepositoryStructure Structure { get; }

        public static RepositoryConfigurationState ZeroState { get; } = new RepositoryConfigurationState(
            isCurrentWithDisk: false,
            isPulled: false,
            isPushed: false,
            lastError: RepositoryConfigurationLastError.None,
            configuration: new RepositoryConfiguration(),
            structure: new RepositoryStructure(ImmutableSortedDictionary<string, BranchReserve>.Empty));

        public RepositoryConfigurationState With(
            bool? isCurrentWithDisk = null,
            bool? isPushed = null,
            bool? isPulled = null,
            RepositoryConfigurationLastError? lastError = null,
            RepositoryConfiguration configuration = null,
            RepositoryStructure structure = null)
        {
            if ((isCurrentWithDisk ?? IsCurrentWithDisk) != IsCurrentWithDisk
                || (isPushed ?? IsPushed) != IsPushed
                || (isPulled ?? IsPulled) != IsPulled
                || (lastError ?? LastError) != LastError 
                || (configuration ?? Configuration) != Configuration 
                || (structure ?? Structure) != Structure)
            {
                return new RepositoryConfigurationState(
                    isCurrentWithDisk: isCurrentWithDisk ?? IsCurrentWithDisk,
                    isPushed: isPushed ?? IsPushed,
                    isPulled: isPulled ?? IsPulled,
                    lastError: lastError ?? LastError,
                    configuration: configuration ?? Configuration,
                    structure: structure ?? Structure);
            }
            return this;
        }
    }
}