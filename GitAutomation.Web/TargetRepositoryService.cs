using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GitAutomation.DomainModels;
using GitAutomation.Scripting;
using GitAutomation.State;
using GitAutomation.Web.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static GitAutomation.DomainModels.TargetRepositoryState.TimestampType;

namespace GitAutomation.Web
{
    public class TargetRepositoryService : IDisposable
    {
        private readonly TargetRepositoryOptions options;
        private readonly IScriptInvoker scriptInvoker;
        private readonly IDispatcher dispatcher;
        private readonly IDisposable subscription;
        public ScriptProgress? LastFetchResult { get; private set; }
        private ImmutableSortedDictionary<TargetRepositoryState.TimestampType, DateTimeOffset> lastTimestamps = ImmutableSortedDictionary<TargetRepositoryState.TimestampType, DateTimeOffset>.Empty;
        private readonly ActionBlock<Unit> changeProcessor;

        public TargetRepositoryService(IOptions<TargetRepositoryOptions> options, IScriptInvoker scriptInvoker, IStateMachine<AppState> stateMachine, IDispatcher dispatcher)
        {
            this.options = options.Value;
            this.scriptInvoker = scriptInvoker;
            this.dispatcher = dispatcher;
            changeProcessor = new ActionBlock<Unit>(_ => DoRepositoryAction());
            subscription = stateMachine.StateUpdates.Select(state => state.Payload.Target).Subscribe(OnStateUpdated);
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
                LoadFromDisk(lastTimestamps[Fetched]);
            }
        }

        internal async Task BeginFetch(DateTimeOffset startTimestamp)
        {
            this.LastFetchResult = scriptInvoker.Invoke(typeof(Scripts.Branches.CloneScript), new Scripts.Branches.CloneScript.CloneScriptParams(startTimestamp), SystemAgent.Instance);
            await LastFetchResult;
        }

        private void LoadFromDisk(DateTimeOffset startTimestamp)
        {
            using var repo = new LibGit2Sharp.Repository(options.CheckoutPath);

            dispatcher.Dispatch(new DomainModels.Actions.RefsAction
            {
                StartTimestamp = startTimestamp,
                AllRefs = repo.Branches.Select(r => new DomainModels.Actions.RefsAction.RefEntry { Name = r.FriendlyName, Commit = r.Tip.Sha }).ToArray()
            }, SystemAgent.Instance);
        }

        void IDisposable.Dispose()
        {
            subscription.Dispose();
        }
    }
}