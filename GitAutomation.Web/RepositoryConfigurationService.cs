using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Configuration;
using GitAutomation.Serialization;
using GitAutomation.Serialization.Defaults;
using GitAutomation.State;
using GitAutomation.Web.Scripts;
using GitAutomation.Web.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static GitAutomation.DomainModels.Configuration.ConfigurationRepositoryState.ConfigurationTimestampType;

namespace GitAutomation.Web
{
    public class RepositoryConfigurationService : IDisposable
    {
        private readonly ConfigRepositoryOptions options;
        private readonly PowerShellScriptInvoker scriptInvoker;
        private readonly ILogger logger;
        private readonly IDispatcher dispatcher;
        private readonly IDisposable subscription;
        private IPowerShellStreams<StandardAction> lastLoadResult;
        private IPowerShellStreams<StandardAction> lastPushResult;
        private Meta meta;
        private DateTimeOffset lastNeedPullTimestamp;
        private DateTimeOffset lastPulledTimestamp;
        private DateTimeOffset lastStoredFieldModifiedTimestamp;

        private readonly struct ConfigurationChange
        {
            public ConfigurationChange(DateTimeOffset timestamp, ConfigurationRepositoryState state, IAgentSpecification modifiedBy)
            {
                Timestamp = timestamp;
                State = state;
                ModifiedBy = modifiedBy;
            }

            public DateTimeOffset Timestamp { get; }
            public ConfigurationRepositoryState State { get; }
            public IAgentSpecification ModifiedBy { get; }

        }


        // TODO - this would be better with an action block
        private readonly BufferBlock<ConfigurationChange> configurationChanges = new BufferBlock<ConfigurationChange>();

        public RepositoryConfigurationService(IOptions<ConfigRepositoryOptions> options, PowerShellScriptInvoker scriptInvoker, ILogger<RepositoryConfigurationService> logger, IDispatcher dispatcher, IStateMachine<AppState> stateMachine)
        {
            this.options = options.Value;
            this.scriptInvoker = scriptInvoker;
            this.logger = logger;
            this.dispatcher = dispatcher;
            subscription = stateMachine.StateUpdates
                .Select(state => new { state.State.Configuration, state.LastChangeBy })
                .DistinctUntilChanged(k => k.Configuration)
                .Subscribe(e => OnStateUpdated(e.Configuration, e.LastChangeBy));

            Task.Run(ConfigurationChangeCommitter);
        }

        public void AssertStarted()
        {
            System.Diagnostics.Debug.Assert(subscription != null);
        }

        private void OnStateUpdated(ConfigurationRepositoryState state, IAgentSpecification modifiedBy)
        {
            // FIXME - should I do this with switchmap/cancellation tokens?
            if (state.Timestamps[NeedPull] > state.Timestamps[Pulled])
            {
                if (lastNeedPullTimestamp != state.Timestamps[NeedPull])
                {
                    lastNeedPullTimestamp = state.Timestamps[NeedPull];
                    BeginLoad(state.Timestamps[NeedPull]);
                }
            }
            else if (state.Timestamps[Pulled] > state.Timestamps[LoadedFromDisk])
            {
                if (lastPulledTimestamp != state.Timestamps[Pulled])
                {
                    lastPulledTimestamp = state.Timestamps[Pulled];
                    LoadFromDisk(state.Timestamps[Pulled]);
                }
            }
            else if (state.Timestamps[StoredFieldModified] > state.Timestamps[Pushed])
            {
                if (lastStoredFieldModifiedTimestamp != state.Timestamps[StoredFieldModified])
                {
                    lastStoredFieldModifiedTimestamp = state.Timestamps[StoredFieldModified];
                    configurationChanges.Post(new ConfigurationChange(state.Timestamps[StoredFieldModified], state, modifiedBy));
                }
            }
        }

        internal async Task BeginLoad(DateTimeOffset startTimestamp)
        {
            this.lastLoadResult = scriptInvoker.Invoke("$/Config/clone.ps1", new { startTimestamp }, options, SystemAgent.Instance);
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
            try
            {
                await Task.WhenAll(config, structure);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to load configuration");
            }
            dispatcher.Dispatch(new StandardAction("ConfigurationRepository:Loaded", new Dictionary<string, object> { { "configuration", config.Result }, { "structure", structure.Result }, { "startTimestamp", startTimestamp } }), SystemAgent.Instance);
            if (!exists)
            {
                // TODO - this action should probably be combined with updating the store
                dispatcher.Dispatch(new StandardAction("ConfigurationRepository:Written", new Dictionary<string, object> { { "startTimestamp", startTimestamp } }), SystemAgent.Instance);
            }
        }

        private async Task CreateDefaultConfiguration(DateTimeOffset startTimestamp)
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
        }

        private async Task ConfigurationChangeCommitter()
        {
            while (await configurationChanges.OutputAvailableAsync())
            {
                var change = await configurationChanges.ReceiveAsync();
                // TODO - separate commit/push, but consider pulling before pushing... causing us to backslide "committed" changes. Maybe we shouldn't separate commit/push.
                await PushToRemote(change.Timestamp, change.State, change.ModifiedBy);
            }
        }

        private async Task PushToRemote(DateTimeOffset startTimestamp, ConfigurationRepositoryState state, IAgentSpecification modifiedBy)
        {
            await Task.WhenAll(
                SerializationUtils.SaveConfigurationAsync(meta, state.Configuration),
                SerializationUtils.SaveStructureAsync(meta, state.Structure)
            );

            // TODO - convert agent to user name/email
            lastPushResult = scriptInvoker.Invoke("$/Config/commitAndPush.ps1", new { startTimestamp }, options, SystemAgent.Instance);
            await lastPushResult;
        }

        void IDisposable.Dispose()
        {
            subscription.Dispose();
        }
    }
}