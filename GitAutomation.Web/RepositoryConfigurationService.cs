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
using GitAutomation.DomainModels.Configuration.Actions;
using GitAutomation.Scripting;
using GitAutomation.Serialization;
using GitAutomation.Serialization.Defaults;
using GitAutomation.State;
using GitAutomation.Web.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using static GitAutomation.DomainModels.Configuration.ConfigurationRepositoryState.ConfigurationTimestampType;

namespace GitAutomation.Web
{
    public class RepositoryConfigurationService : IDisposable
    {
        private readonly ConfigRepositoryOptions options;
        private readonly IScriptInvoker scriptInvoker;
        private readonly ILogger logger;
        private readonly IDispatcher dispatcher;
        private readonly IDisposable subscription;
        public ScriptProgress LastLoadResult { get; private set; }
        public ScriptProgress LastPushResult { get; private set; }
        private Meta meta;

        private readonly struct ConfigurationChange
        {
            public ConfigurationChange(DateTimeOffset timestamp, StateUpdateEvent<ConfigurationRepositoryState> e)
            {
                Timestamp = timestamp;
                Event = e;
            }

            public DateTimeOffset Timestamp { get; }
            public StateUpdateEvent<ConfigurationRepositoryState> Event { get; }

        }


        private ImmutableSortedDictionary<ConfigurationRepositoryState.ConfigurationTimestampType, DateTimeOffset> lastTimestamps;
        private readonly BufferBlock<ConfigurationChange> configurationChanges = new BufferBlock<ConfigurationChange>();
        private readonly ActionBlock<Unit> changeProcessor;

        public RepositoryConfigurationService(IOptions<ConfigRepositoryOptions> options, IScriptInvoker scriptInvoker, ILogger<RepositoryConfigurationService> logger, IDispatcher dispatcher, IStateMachine<AppState> stateMachine)
        {
            this.options = options.Value;
            this.scriptInvoker = scriptInvoker;
            this.logger = logger;
            this.dispatcher = dispatcher;
            changeProcessor = new ActionBlock<Unit>(_ => DoRepositoryAction());
            subscription = stateMachine.StateUpdates
                .Select(state => state.WithState(state.Payload.Configuration))
                .DistinctUntilChanged(k => k.Payload)
                .Subscribe(e => OnStateUpdated(e));
        }

        public void AssertStarted()
        {
            System.Diagnostics.Debug.Assert(subscription != null);
        }

        private void OnStateUpdated(StateUpdateEvent<ConfigurationRepositoryState> e)
        {
            if (lastTimestamps != null && lastTimestamps[StoredFieldModified] != e.Payload.Timestamps[StoredFieldModified])
            {
                configurationChanges.Post(new ConfigurationChange(e.Payload.Timestamps[StoredFieldModified], e));
            }

            lastTimestamps = e.Payload.Timestamps;
            changeProcessor.Post(Unit.Default);
        }

        private async Task DoRepositoryAction()
        {
            if (lastTimestamps[LoadedFromDisk] > DateTimeOffset.MinValue && lastTimestamps[StoredFieldModified] > lastTimestamps[Pushed])
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
            this.LastLoadResult = scriptInvoker.Invoke("$/Config/clone.ps1", new { startTimestamp }, options, SystemAgent.Instance);
            await LastLoadResult;
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
            dispatcher.Dispatch(new StateUpdateEvent<IStandardAction>(new ConfigurationLoadedAction { Configuration= config.Result, Structure= structure.Result, StartTimestamp = startTimestamp }, SystemAgent.Instance, "Loaded from disk"));
            // clear out any interim config changes, as they're now out of date...
            configurationChanges.TryReceiveAll(out var _);
            if (!exists)
            {
                // this action is not combined with updating the store as the Loaded action does not know that we aren't overriding it from a normal load
                dispatcher.Dispatch(new StateUpdateEvent<IStandardAction>(new ConfigurationWrittenAction { StartTimestamp = startTimestamp }, SystemAgent.Instance, "Initialize configuration with defaults"));
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
                    await CommitChange(change.Timestamp, change.Event);
                }
                await PushToRemote(changes.Select(c => c.Timestamp).Max());
            }
        }

        private async Task CommitChange(DateTimeOffset startTimestamp, StateUpdateEvent<ConfigurationRepositoryState> e)
        {
            await Task.WhenAll(
                SerializationUtils.SaveConfigurationAsync(meta, e.Payload.Configuration),
                SerializationUtils.SaveStructureAsync(meta, e.Payload.Structure)
            );

            // TODO - convert agent to user name/email
            LastPushResult = scriptInvoker.Invoke("$/Config/commit.ps1", new { startTimestamp, comment = e.Comment }, options, SystemAgent.Instance);
            // TODO - I'm concerned there's a race condition in here given that committed changes are cleared out after a load from disk, not after fetch...
            await LastPushResult;
        }

        private async Task PushToRemote(DateTimeOffset startTimestamp)
        {
            LastPushResult = scriptInvoker.Invoke("$/Config/push.ps1", new { startTimestamp }, options, SystemAgent.Instance);
            await LastPushResult;
        }

        void IDisposable.Dispose()
        {
            subscription.Dispose();
        }
    }
}