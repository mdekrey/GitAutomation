using System;
using System.Threading.Tasks;
using GitAutomation.DomainModels;
using GitAutomation.Serialization.Defaults;
using GitAutomation.Web.Scripts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static GitAutomation.Web.RepositoryConfigurationState;

namespace GitAutomation.Web
{
    internal class RepositoryConfigurationService : IDispatcher
    {
        private readonly ConfigRepositoryOptions options;
        private readonly PowerShellScriptInvoker scriptInvoker;
        private readonly ILogger logger;
        private RepositoryConfigurationState state = RepositoryConfigurationState.ZeroState;
        private PowerShellStreams<StandardAction> lastLoadResult;
        private PowerShellStreams<StandardAction> lastPushResult;

        public RepositoryConfigurationService(IOptions<ConfigRepositoryOptions> options, PowerShellScriptInvoker scriptInvoker, ILogger<RepositoryConfigurationService> logger)
        {
            this.options = options.Value;
            this.scriptInvoker = scriptInvoker;
            this.logger = logger;
        }

        internal async void BeginLoad()
        {
            this.lastLoadResult = scriptInvoker.Invoke("$/Config/clone.ps1", new { }, options);
        }

        public void Dispatch(StandardAction action)
        {
            state = (action.Action switch
            {
                "ConfigurationDirectoryNotAccessible" => ConfigurationDirectoryNotAccessible(state),
                "ConfigurationReadyToLoad" => ConfigurationReadyToLoad(state),
                "ConfigurationRepositoryCouldNotBeCloned" => ConfigurationRepositoryCouldNotBeCloned(state),
                "ConfigurationRepositoryPasswordIncorrect" => ConfigurationRepositoryPasswordIncorrect(state),
                "ConfigurationRepositoryNoBranch" => ConfigurationRepositoryNoBranch(state),
                "ConfigurationRepositoryCouldNotCommit" => ConfigurationRepositoryCouldNotCommit(state),
                "ConfigurationRepositoryCouldNotPush" => ConfigurationRepositoryCouldNotPush(state),
                "ConfigurationPushSuccess" => ConfigurationPushSuccess(state),
                _ => state,
            }).With(structure: RepositoryStructureReducer.Reduce(state.Structure, action));
        }

        private RepositoryConfigurationState ConfigurationRepositoryNoBranch(RepositoryConfigurationState original)
        {
            Task.Run(() => CreateDefaultConfiguration());

            return original.With(isPulled: true);
        }

        private RepositoryConfigurationState ConfigurationRepositoryPasswordIncorrect(RepositoryConfigurationState original) =>
            original.With(isCurrentWithDisk: false, isPulled: false, lastError: RepositoryConfigurationLastError.Error_PasswordIncorrect);

        private RepositoryConfigurationState ConfigurationRepositoryCouldNotBeCloned(RepositoryConfigurationState original) =>
            original.With(isCurrentWithDisk: false, isPulled: false, lastError: RepositoryConfigurationLastError.Error_FailedToClone);

        private RepositoryConfigurationState ConfigurationDirectoryNotAccessible(RepositoryConfigurationState original) =>
            original.With(isCurrentWithDisk: false, isPulled: false, lastError: RepositoryConfigurationLastError.Error_DirectoryNotAccessible);

        private RepositoryConfigurationState ConfigurationReadyToLoad(RepositoryConfigurationState original)
        {
            Task.Run(() => LoadFromDisk());

            return original.With(isPushed: true, isPulled: true, isCurrentWithDisk: false);
        }

        private RepositoryConfigurationState ConfigurationRepositoryCouldNotCommit(RepositoryConfigurationState original) =>
            original.With(isPushed: false, lastError: RepositoryConfigurationLastError.Error_FailedToCommit);

        private RepositoryConfigurationState ConfigurationRepositoryCouldNotPush(RepositoryConfigurationState original) =>
            original.With(isPushed: false, lastError: RepositoryConfigurationLastError.Error_FailedToPush);

        private RepositoryConfigurationState ConfigurationPushSuccess(RepositoryConfigurationState original) =>
            original.With(isPushed: true);

        private async Task CreateDefaultConfiguration()
        {
            try
            {
                await DefaultsWriter.WriteDefaultsToDirectory(options.CheckoutPath);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Could not write default configuration");
                return;
            }

            LoadFromDisk();
            PushToRemote();

        }

        private void LoadFromDisk()
        {
            // TODO - load from disk
        }

        private void PushToRemote()
        {
            lastPushResult = scriptInvoker.Invoke("$/Config/commitAndPush.ps1", new { }, options);
        }
    }
}