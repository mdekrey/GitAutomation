using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitAutomation.DomainModels;
using GitAutomation.Serialization;
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
        private readonly IDispatcher dispatcher;
        private RepositoryConfigurationState state = RepositoryConfigurationState.ZeroState;
        private PowerShellStreams<StandardAction> lastLoadResult;
        private PowerShellStreams<StandardAction> lastPushResult;
        private Meta meta;

        public RepositoryConfigurationService(IOptions<ConfigRepositoryOptions> options, PowerShellScriptInvoker scriptInvoker, ILogger<RepositoryConfigurationService> logger, IDispatcher dispatcher)
        {
            this.options = options.Value;
            this.scriptInvoker = scriptInvoker;
            this.logger = logger;
            this.dispatcher = dispatcher;
        }

        internal void BeginLoad()
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
                "ConfigurationLoaded" => ConfigurationLoaded(state, (RepositoryConfiguration)action.Payload["configuration"], (RepositoryStructure)action.Payload["structure"]),
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

        private RepositoryConfigurationState ConfigurationLoaded(RepositoryConfigurationState original, RepositoryConfiguration repositoryConfiguration, RepositoryStructure repositoryStructure) =>
            original.With(isCurrentWithDisk: true, configuration: repositoryConfiguration, structure: repositoryStructure);

        private async Task CreateDefaultConfiguration()
        {
            var newOrphanBranch = await scriptInvoker.Invoke("$/Config/newOrphanBranch.ps1", new { }, options);

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

        private async void LoadFromDisk()
        {
            meta = await SerializationUtils.LoadMetaAsync(options.CheckoutPath);
            var config = SerializationUtils.LoadConfigurationAsync(meta);
            var structure = SerializationUtils.LoadStructureAsync(meta);
            await Task.WhenAll(config, structure);
            dispatcher.Dispatch(new StandardAction("ConfigurationLoaded", new Dictionary<string, object> { { "configuration", config.Result }, { "structure", structure.Result } }));
        }

        private async void PushToRemote()
        {
            lastPushResult = scriptInvoker.Invoke("$/Config/commitAndPush.ps1", new { }, options);
            await lastPushResult;
        }
    }
}