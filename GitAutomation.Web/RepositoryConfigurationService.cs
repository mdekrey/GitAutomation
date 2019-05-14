using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitAutomation.DomainModels;
using GitAutomation.Serialization;
using GitAutomation.Serialization.Defaults;
using GitAutomation.Web.Scripts;
using GitAutomation.Web.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GitAutomation.Web
{
    public class RepositoryConfigurationService : IDisposable
    {
        private readonly ConfigRepositoryOptions options;
        private readonly PowerShellScriptInvoker scriptInvoker;
        private readonly ILogger logger;
        private readonly IDispatcher dispatcher;
        private readonly IStateMachine stateMachine;
        private readonly IDisposable subscription;
        private IPowerShellStreams<StandardAction> lastLoadResult;
        private IPowerShellStreams<StandardAction> lastPushResult;
        private Meta meta;
        private DateTimeOffset lastNeedPullTimestamp;
        private DateTimeOffset lastPulledTimestamp;
        private DateTimeOffset lastStoredFieldModifiedTimestamp;

        public RepositoryConfigurationService(IOptions<ConfigRepositoryOptions> options, PowerShellScriptInvoker scriptInvoker, ILogger<RepositoryConfigurationService> logger, IDispatcher dispatcher, IStateMachine stateMachine)
        {
            this.options = options.Value;
            this.scriptInvoker = scriptInvoker;
            this.logger = logger;
            this.dispatcher = dispatcher;
            this.stateMachine = stateMachine;
            subscription = stateMachine.StateUpdates.Subscribe(OnStateUpdated);
        }

        public void AssertStarted()
        {
            System.Diagnostics.Debug.Assert(subscription != null);
        }

        private void OnStateUpdated(RepositoryConfigurationState state)
        {
            // FIXME - should I do this with switchmap/cancellation tokens?
            if (state.NeedPullTimestamp > state.PulledTimestamp)
            {
                if (lastNeedPullTimestamp != state.NeedPullTimestamp)
                {
                    lastNeedPullTimestamp = state.NeedPullTimestamp;
                    BeginLoad(state.NeedPullTimestamp);
                }
            }
            else if (state.PulledTimestamp > state.LoadedFromDiskTimestamp)
            {
                if (lastPulledTimestamp != state.PulledTimestamp)
                {
                    lastPulledTimestamp = state.PulledTimestamp;
                    LoadFromDisk(state.PulledTimestamp);
                }
            }
            else if (state.StoredFieldModifiedTimestamp > state.PushedTimestamp)
            {
                if (lastStoredFieldModifiedTimestamp != state.StoredFieldModifiedTimestamp)
                {
                    lastStoredFieldModifiedTimestamp = state.StoredFieldModifiedTimestamp;
                    PushToRemote(state.StoredFieldModifiedTimestamp);
                }
            }
        }

        internal async Task BeginLoad(DateTimeOffset startTimestamp)
        {
            this.lastLoadResult = scriptInvoker.Invoke("$/Config/clone.ps1", new { startTimestamp }, options);
            await lastLoadResult;
        }

        private async Task LoadFromDisk(DateTimeOffset startTimestamp)
        {
            var exists = SerializationUtils.MetaExists(options.CheckoutPath);
            if (!exists)
            {
                await CreateDefaultConfiguration(startTimestamp);
            }

            meta = await SerializationUtils.LoadMetaAsync(options.CheckoutPath);
            var config = SerializationUtils.LoadConfigurationAsync(meta);
            var structure = SerializationUtils.LoadStructureAsync(meta);
            await Task.WhenAll(config, structure);
            dispatcher.Dispatch(new StandardAction("ConfigurationLoaded", new Dictionary<string, object> { { "configuration", config.Result }, { "structure", structure.Result }, { "startTimestamp", startTimestamp } }));
            if (!exists)
            {
                // TODO - this action should probably be combined with updating the store
                dispatcher.Dispatch(new StandardAction("ConfigurationWritten", new Dictionary<string, object> { { "startTimestamp", startTimestamp } }));
            }
        }

        private async Task CreateDefaultConfiguration(DateTimeOffset startTimestamp)
        {
            await scriptInvoker.Invoke("$/Config/newOrphanBranch.ps1", new { startTimestamp }, options);

            try
            {
                await DefaultsWriter.WriteDefaultsToDirectory(options.CheckoutPath);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Could not write default configuration");
                return;
            }
        }

        private async Task PushToRemote(DateTimeOffset startTimestamp)
        {
            lastPushResult = scriptInvoker.Invoke("$/Config/commitAndPush.ps1", new { startTimestamp }, options);
            await lastPushResult;
        }

        void IDisposable.Dispose()
        {
            subscription.Dispose();
        }
    }
}