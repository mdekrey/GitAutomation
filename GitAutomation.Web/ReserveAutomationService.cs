using GitAutomation.DomainModels;
using GitAutomation.State;
using GitAutomation.Web.Scripts;
using GitAutomation.Web.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GitAutomation.Web
{
    public class ReserveAutomationService
    {
        private readonly TargetRepositoryOptions options;
        private readonly PowerShellScriptInvoker scriptInvoker;
        private readonly ILogger logger;
        private readonly IStateMachine<AppState> stateMachine;
        private readonly IDisposable subscription;
        private IPowerShellStreams<StandardAction> lastFetchResult;
        private IPowerShellStreams<StandardAction> lastLoadFromDiskResult;
        private readonly ConcurrentDictionary<string, SingleReserveAutomation> reserves = new ConcurrentDictionary<string, SingleReserveAutomation>();
        private readonly ActionBlock<string> reserveProcessor;

        public IPowerShellStreams<StandardAction> LastScript { get; private set; }

        readonly struct ReserveFullState
        {
            public ReserveFullState(BranchReserve reserve, IEnumerable<string> branchDetails, IEnumerable<BranchReserve> upstreamReserves)
            {
                Reserve = reserve;
                BranchDetails = branchDetails;
                UpstreamReserves = upstreamReserves;
            }

            public BranchReserve Reserve { get; }
            public IEnumerable<string> BranchDetails { get; }
            public IEnumerable<BranchReserve> UpstreamReserves { get; }
            public bool IsValid => !UpstreamReserves.Any(r => r == null);
        }

        class SingleReserveAutomation : IDisposable
        {
            private readonly IStateMachine<AppState> stateMachine;
            private readonly ReserveAutomationService service;
            private readonly IDisposable subscription;

            public string Name { get; }
            public ReserveFullState Data { get; private set; }

            public SingleReserveAutomation(string name, IStateMachine<AppState> stateMachine, ReserveAutomationService service)
            {
                this.Name = name;
                this.stateMachine = stateMachine;
                this.service = service;
                subscription = stateMachine.StateUpdates
                    .Select(state =>
                    {
                        var reserves = state.State.Configuration.Structure.BranchReserves;
                        var branches = state.State.Target.Branches;
                        var reserve = reserves[name];
                        var branchDetails = reserve.IncludedBranches.Keys.Select(k => branches.ContainsKey(k) ? branches[k] : BranchReserve.EmptyCommit);
                        var upstreamReserves = reserve.Upstream.Keys.Select(upstream => reserves.ContainsKey(upstream) ? reserves[upstream] : null);
                        return new ReserveFullState (reserve, branchDetails, upstreamReserves );
                    })
                    // TODO - this isn't fast, but it does work
                    .DistinctUntilChanged(e => JsonConvert.SerializeObject(e))
                    .Subscribe(data =>
                    {
                        ReceiveNewState(data);
                    });
            }

            private void ReceiveNewState(ReserveFullState data)
            {
                this.Data = data;
                service.AddReserveProcessing(Name);
            }

            public void Dispose()
            {
                subscription.Dispose();
            }
        }

        private void AddReserveProcessing(string name)
        {
            reserveProcessor.Post(name);
        }

        public ReserveAutomationService(IOptions<TargetRepositoryOptions> options, PowerShellScriptInvoker scriptInvoker, ILogger<TargetRepositoryService> logger, IStateMachine<AppState> stateMachine)
        {
            this.options = options.Value;
            this.scriptInvoker = scriptInvoker;
            this.logger = logger;
            this.stateMachine = stateMachine;
            reserveProcessor = new ActionBlock<string>(ProcessReserve);
            subscription = stateMachine.StateUpdates.Subscribe(OnStateUpdated);
        }

        public void AssertStarted()
        {
            System.Diagnostics.Debug.Assert(subscription != null);
        }

        private async Task ProcessReserve(string reserveName)
        {
            // guard against deleting the reserve while queued
            if (reserves.TryGetValue(reserveName, out var reserveFullState))
            {
                // if we're not valid, don't process anything
                if (!reserveFullState.Data.IsValid)
                {
                    return;
                }
                var reserve = reserveFullState.Data.Reserve;
                var scripts = stateMachine.State.Configuration.Configuration.ReserveTypes[reserve.ReserveType].StateScripts;
                if (scripts.TryGetValue(reserve.Status, out var scriptName))
                {
                    LastScript = await scriptInvoker.Invoke(scriptName, reserveFullState, new { /* TODO - we will need to access git */ }, SystemAgent.Instance);
                }
            }
        }

        private void OnStateUpdated(StateUpdateEvent<AppState> obj)
        {
            foreach (var key in obj.State.Configuration.Structure.BranchReserves.Keys)
            {
                reserves.GetOrAdd(key, AutomationFactory);
            }
            foreach (var entry in reserves.Keys.Except(obj.State.Configuration.Structure.BranchReserves.Keys).ToArray())
            {
                if (reserves.Remove(entry, out var removed))
                {
                    removed.Dispose();
                }
            }
        }

        private SingleReserveAutomation AutomationFactory(string name)
        {
            return new SingleReserveAutomation(name, stateMachine, this);
        }
    }
}
