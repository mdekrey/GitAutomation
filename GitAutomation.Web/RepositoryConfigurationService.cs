using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
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


        private ImmutableSortedDictionary<ConfigurationRepositoryState.ConfigurationTimestampType, DateTimeOffset> lastTimestamps;
        private readonly BufferBlock<ConfigurationChange> configurationChanges = new BufferBlock<ConfigurationChange>();
        private readonly ActionBlock<Unit> changeProcessor;

        public RepositoryConfigurationService(IOptions<ConfigRepositoryOptions> options, PowerShellScriptInvoker scriptInvoker, ILogger<RepositoryConfigurationService> logger, IDispatcher dispatcher, IStateMachine<AppState> stateMachine)
        {
            this.options = options.Value;
            this.scriptInvoker = scriptInvoker;
            this.logger = logger;
            this.dispatcher = dispatcher;
            changeProcessor = new ActionBlock<Unit>(_ => DoRepositoryAction());
            subscription = stateMachine.StateUpdates
                .Select(state => new { state.State.Configuration, state.LastChangeBy })
                .DistinctUntilChanged(k => k.Configuration)
                .Subscribe(e => OnStateUpdated(e.Configuration, e.LastChangeBy));
        }

        public void AssertStarted()
        {
            System.Diagnostics.Debug.Assert(subscription != null);
        }

        private void OnStateUpdated(ConfigurationRepositoryState state, IAgentSpecification modifiedBy)
        {
            if (lastTimestamps != null && lastTimestamps[StoredFieldModified] != state.Timestamps[StoredFieldModified])
            {
                configurationChanges.Post(new ConfigurationChange(state.Timestamps[StoredFieldModified], state, modifiedBy));
            }

            lastTimestamps = state.Timestamps;
            changeProcessor.Post(Unit.Default);
        }

        private async Task DoRepositoryAction()
        {
            if (lastTimestamps[StoredFieldModified] > lastTimestamps[Pushed])
            {
                await ConfigurationChangeCommitter();
            }
            else if (lastTimestamps[NeedPull] > lastTimestamps[Pulled])
            {
                await BeginLoad(lastTimestamps[NeedPull]);
            }
            else if (lastTimestamps[Pulled] > lastTimestamps[LoadedFromDisk])
            {
                await LoadFromDisk(lastTimestamps[Pulled]);
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
            // clear out any interim config changes, as they're now out of date...
            configurationChanges.TryReceiveAll(out var _);
            if (!exists)
            {
                // this action is not combined with updating the store as the Loaded action does not know that we aren't overriding it from a normal load
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
            if (configurationChanges.TryReceiveAll(out var changes))
            {
                foreach (var change in changes)
                {
                    await CommitChange(change.Timestamp, change.State, change.ModifiedBy);
                }
                await PushToRemote(changes.Select(c => c.Timestamp).Max());
            }
        }

        private async Task CommitChange(DateTimeOffset startTimestamp, ConfigurationRepositoryState state, IAgentSpecification modifiedBy)
        {
            await Task.WhenAll(
                SerializationUtils.SaveConfigurationAsync(meta, state.Configuration),
                SerializationUtils.SaveStructureAsync(meta, state.Structure)
            );

            // TODO - convert agent to user name/email
            lastPushResult = scriptInvoker.Invoke("$/Config/commit.ps1", new { startTimestamp }, options, SystemAgent.Instance);
            await lastPushResult;
        }

        private async Task PushToRemote(DateTimeOffset startTimestamp)
        {
            lastPushResult = scriptInvoker.Invoke("$/Config/push.ps1", new { startTimestamp }, options, SystemAgent.Instance);
            await lastPushResult;
        }

        void IDisposable.Dispose()
        {
            subscription.Dispose();
        }
    }
}