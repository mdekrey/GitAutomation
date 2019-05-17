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
using static GitAutomation.DomainModels.TargetRepositoryState.TimestampType;

namespace GitAutomation.Web
{
    public class TargetRepositoryService : IDisposable
    {
        private readonly TargetRepositoryOptions options;
        private readonly PowerShellScriptInvoker scriptInvoker;
        private readonly ILogger logger;
        private readonly IDisposable subscription;
        private IPowerShellStreams<StandardAction> lastFetchResult;
        private IPowerShellStreams<StandardAction> lastLoadFromDiskResult;
        private DateTimeOffset lastNeedFetchTimestamp;
        private DateTimeOffset lastFetchedTimestamp;

        public TargetRepositoryService(IOptions<TargetRepositoryOptions> options, PowerShellScriptInvoker scriptInvoker, ILogger<TargetRepositoryService> logger, IStateMachine<AppState> stateMachine)
        {
            this.options = options.Value;
            this.scriptInvoker = scriptInvoker;
            this.logger = logger;
            subscription = stateMachine.StateUpdates.Select(state => state.Target).Subscribe(OnStateUpdated);
        }

        public void AssertStarted()
        {
            System.Diagnostics.Debug.Assert(subscription != null);
        }

        private void OnStateUpdated(TargetRepositoryState state)
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
            this.lastLoadFromDiskResult = scriptInvoker.Invoke("$/Repository/gitBranchStates.ps1", new { startTimestamp }, options);
            await lastLoadFromDiskResult;
        }

        void IDisposable.Dispose()
        {
            subscription.Dispose();
        }
    }
}