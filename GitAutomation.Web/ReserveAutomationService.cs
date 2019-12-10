using GitAutomation.DomainModels;
using GitAutomation.Scripting;
using GitAutomation.State;
using GitAutomation.Web.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GitAutomation.Web
{
    public class ReserveAutomationService
    {
        private readonly AutomationOptions automationOptions;
        private readonly IScriptInvoker scriptInvoker;
        private readonly ILogger logger;
        private readonly IStateMachine<AppState> stateMachine;
        private readonly IDisposable subscription;
        private readonly ConcurrentDictionary<string, SingleReserveAutomation> reserves = new ConcurrentDictionary<string, SingleReserveAutomation>();
        private readonly ActionBlock<string> reserveProcessor;

        class SingleReserveAutomation : IDisposable
        {
            private readonly ReserveAutomationService service;
            private readonly IDisposable subscription;

            public string Name { get; }
            public ReserveFullState Data { get; private set; }
            public ScriptProgress? LastScript { get; set; }

            public SingleReserveAutomation(string name, IStateMachine<AppState> stateMachine, ReserveAutomationService service)
            {
                this.Name = name;
                this.service = service;
                subscription = stateMachine.StateUpdates
                    .Select(state =>
                    {
                        var reserves = state.Payload.Configuration.Structure.BranchReserves;
                        var branches = state.Payload.Target.Branches;
                        var reserve = reserves[name];
                        var branchDetails = reserve.IncludedBranches.Keys.ToDictionary(k => k, k => branches.ContainsKey(k) ? branches[k] : BranchReserve.EmptyCommit);
                        var upstreamReserves = reserve.Upstream.Keys.ToDictionary(k => k, upstream => reserves.ContainsKey(upstream) ? reserves[upstream] : null);
                        return new ReserveFullState(reserve, branchDetails, upstreamReserves!);
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

        public ReserveAutomationService(IOptions<AutomationOptions> automationOptions, IScriptInvoker scriptInvoker, ILogger<TargetRepositoryService> logger, IStateMachine<AppState> stateMachine)
        {
            this.automationOptions = automationOptions.Value;
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

        public ScriptProgress? LastScriptForReserve(string reserveName)
        {
            if (reserves.TryGetValue(reserveName, out var reserveFullState))
            {
                return reserveFullState.LastScript;
            }

            return null;
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
                    var path = Path.Combine(automationOptions.WorkspacePath, Path.GetRandomFileName());
                    Directory.CreateDirectory(path);
                    logger.LogInformation("For '{reserveName}' with status '{Status}' as type '{ReserveType}', run script '{scriptName}' in path '{path}'", reserveName, reserve.Status, reserve.ReserveType, scriptName, path);
                    try
                    {
                        reserveFullState.LastScript = scriptInvoker.Invoke(
                            scriptInvoker.GetScript<ReserveScriptParameters>(scriptName),
                            new ReserveScriptParameters(
                                reserveFullState.Name,
                                reserveFullState.Data,
                                workingPath: path
                            ),
                            SystemAgent.Instance
                        );
                        await reserveFullState.LastScript;
                        logger.LogInformation("For '{reserveName}' with status '{Status}' as type '{ReserveType}', successfully ran script '{scriptName}' in path '{path}'", reserveName, reserve.Status, reserve.ReserveType, scriptName, path);
                    }
                    finally
                    {
                        Directory.Delete(path, recursive: true);
                    }
                }
            }
        }

        private void OnStateUpdated(StateUpdateEvent<AppState> obj)
        {
            foreach (var key in obj.Payload.Configuration.Structure.BranchReserves.Keys)
            {
                reserves.GetOrAdd(key, AutomationFactory);
            }
            foreach (var entry in reserves.Keys.Except(obj.Payload.Configuration.Structure.BranchReserves.Keys).ToArray())
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
