using GitAutomation.DomainModels;
using System.Collections.Immutable;

namespace GitAutomation.Web
{
    internal class RepositoryConfigurationState
    {
        public RepositoryConfigurationState(RepositoryConfigurationStatus status, RepositoryConfiguration configuration, RepositoryStructure structure)
        {
            Status = status;
            Configuration = configuration;
            Structure = structure;
        }

        public enum RepositoryConfigurationStatus
        {
            NotReady,
            Error_DirectoryNotAccessible,
            Error_FailedToClone,
            Error_PasswordIncorrect,
            Ready
        }

        public RepositoryConfigurationStatus Status { get; }
        public RepositoryConfiguration Configuration { get; }
        public RepositoryStructure Structure { get; }

        public static RepositoryConfigurationState ZeroState { get; } = new RepositoryConfigurationState(
            RepositoryConfigurationStatus.NotReady,
            new RepositoryConfiguration(),
            new RepositoryStructure(ImmutableSortedDictionary<string, BranchReserve>.Empty));

        public RepositoryConfigurationState With(
            RepositoryConfigurationStatus? status = null,
            RepositoryConfiguration configuration = null,
            RepositoryStructure structure = null)
        {
            if ((status ?? Status) != Status || (configuration ?? Configuration) != Configuration || (structure ?? Structure) != Structure)
            {
                return new RepositoryConfigurationState(
                    status ?? Status,
                    configuration ?? Configuration,
                    structure ?? Structure);
            }
            return this;
        }
    }
}