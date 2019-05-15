using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GitAutomation.DomainModels;
using GitAutomation.Serialization;
using GitAutomation.Serialization.Defaults;
using GitAutomation.Web.Scripts;
using GitAutomation.Web.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static GitAutomation.DomainModels.TargetBranchesState.TimestampType;

namespace GitAutomation.Web
{
    public class TargetBranchesService : IDisposable
    {
        private readonly TargetRepositoryOptions options;
        private readonly PowerShellScriptInvoker scriptInvoker;
        private readonly ILogger logger;
        private readonly IDispatcher dispatcher;
        private readonly IStateMachine<AppState> stateMachine;
        private readonly IDisposable subscription;
        private IPowerShellStreams<StandardAction> lastFetchResult;
        private IPowerShellStreams<StandardAction> lastLoadFromDiskResult;
        private Meta meta;
        private DateTimeOffset lastNeedFetchTimestamp;
        private DateTimeOffset lastFetchedTimestamp;
        private DateTimeOffset lastStoredFieldModifiedTimestamp;

        public TargetBranchesService(IOptions<TargetRepositoryOptions> options, PowerShellScriptInvoker scriptInvoker, ILogger<TargetBranchesService> logger, IDispatcher dispatcher, IStateMachine<AppState> stateMachine)
        {
            this.options = options.Value;
            this.scriptInvoker = scriptInvoker;
            this.logger = logger;
            this.dispatcher = dispatcher;
            this.stateMachine = stateMachine;
            subscription = stateMachine.StateUpdates.Select(state => state.Target).Subscribe(OnStateUpdated);
        }

        public void AssertStarted()
        {
            System.Diagnostics.Debug.Assert(subscription != null);
        }

        private void OnStateUpdated(TargetBranchesState state)
        {
            // FIXME - should I do this with switchmap/cancellation tokens?
            if (state.Timestamps[NeededFetch] > state.Timestamps[Fetched])
            {
                if (lastNeedFetchTimestamp != state.Timestamps[NeededFetch])
                {
                    lastNeedFetchTimestamp = state.Timestamps[NeededFetch];
                    BeginFetch(state.Timestamps[NeededFetch]);
                }
            }
            else if (state.Timestamps[Fetched] > state.Timestamps[LoadedFromDisk])
            {
                if (lastFetchedTimestamp != state.Timestamps[Fetched])
                {
                    lastFetchedTimestamp = state.Timestamps[Fetched];
                    LoadFromDisk(state.Timestamps[Fetched]);
                }
            }
        }

        internal async Task BeginFetch(DateTimeOffset startTimestamp)
        {
            this.lastFetchResult = scriptInvoker.Invoke("$/Repository/clone.ps1", new { startTimestamp }, options);
            await lastFetchResult;
        }

        private async Task LoadFromDisk(DateTimeOffset startTimestamp)
        {
            this.lastFetchResult = scriptInvoker.Invoke("$/Repository/gitBranchStates.ps1", new { startTimestamp }, options);
            await lastFetchResult;
        }

        void IDisposable.Dispose()
        {
            subscription.Dispose();
        }
    }
}