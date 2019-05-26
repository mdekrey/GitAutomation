using System;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GitAutomation.DomainModels;
using GitAutomation.State;
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
        public IPowerShellStreams<PowerShellLine> LastFetchResult { get; private set; }
        public IPowerShellStreams<PowerShellLine> LastLoadFromDiskResult { get; private set; }
        private ImmutableSortedDictionary<TargetRepositoryState.TimestampType, DateTimeOffset> lastTimestamps;
        private readonly ActionBlock<Unit> changeProcessor;

        public TargetRepositoryService(IOptions<TargetRepositoryOptions> options, PowerShellScriptInvoker scriptInvoker, ILogger<TargetRepositoryService> logger, IStateMachine<AppState> stateMachine)
        {
            this.options = options.Value;
            this.scriptInvoker = scriptInvoker;
            this.logger = logger;
            changeProcessor = new ActionBlock<Unit>(_ => DoRepositoryAction());
            subscription = stateMachine.StateUpdates.Select(state => state.State.Target).Subscribe(OnStateUpdated);
        }

        public void AssertStarted()
        {
            System.Diagnostics.Debug.Assert(subscription != null);
        }

        private void OnStateUpdated(TargetRepositoryState state)
        {
            lastTimestamps = state.Timestamps;
            changeProcessor.Post(Unit.Default);
        }

        private async Task DoRepositoryAction()
        {
            if (lastTimestamps[NeededFetch] > lastTimestamps[Fetched])
            {
                await BeginFetch(lastTimestamps[NeededFetch]);
            }
            else if (lastTimestamps[Fetched] > lastTimestamps[LoadedFromDisk])
            {
                await LoadFromDisk(lastTimestamps[Fetched]);
            }
        }

        internal async Task BeginFetch(DateTimeOffset startTimestamp)
        {
            this.LastFetchResult = scriptInvoker.Invoke("$/Repository/clone.ps1", new { startTimestamp }, options, SystemAgent.Instance);
            await LastFetchResult;
        }

        private async Task LoadFromDisk(DateTimeOffset startTimestamp)
        {
            this.LastLoadFromDiskResult = scriptInvoker.Invoke("$/Repository/gitBranchStates.ps1", new { startTimestamp }, options, SystemAgent.Instance);
            await LastLoadFromDiskResult;
        }

        void IDisposable.Dispose()
        {
            subscription.Dispose();
        }
    }
}