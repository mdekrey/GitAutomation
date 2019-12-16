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
            public ReserveAutomationState Data { get; private set; }
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
                        var branchDetails = reserve.IncludedBranches.Keys.ToImmutableDictionary(k => k, k => branches.ContainsKey(k) ? branches[k] : BranchReserve.EmptyCommit);
                        var upstreamReserves = reserve.Upstream.Keys.ToImmutableDictionary(k => k, upstream => reserves.ContainsKey(upstream) ? reserves[upstream] : null);
                        return new ReserveAutomationState(reserve, branchDetails, upstreamReserves!);
                    })
                    // TODO - this isn't fast, but it does work
                    .DistinctUntilChanged(e => JsonConvert.SerializeObject(e))
                    .Subscribe(data =>
                    {
                        ReceiveNewState(data);
                    });
            }

            private void ReceiveNewState(ReserveAutomationState data)
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
            try
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
                                    reserveFullState.Data.Reserve,
                                    reserveFullState.Data.BranchDetails,
                                    reserveFullState.Data.UpstreamReserves,
                                    workingPath: path
                                ),
                                SystemAgent.Instance
                            );
                            var result = await reserveFullState.LastScript;
                            if (result.Exception == null)
                            {
                                logger.LogInformation("For '{reserveName}' with status '{Status}' as type '{ReserveType}', successfully ran script '{scriptName}' in path '{path}'", reserveName, reserve.Status, reserve.ReserveType, scriptName, path);
                            }
                            else
                            {
                                logger.LogError(result.Exception, "For '{reserveName}' with status '{Status}' as type '{ReserveType}', failed to run script '{scriptName}' in path '{path}'", reserveName, reserve.Status, reserve.ReserveType, scriptName, path);
                            }
                        }
                        finally
                        {
                            SafeDelete(path);
                        }
                    }
                }
            } catch (Exception ex)
            {
                logger.LogError(ex, "Error in reserve automation process block, while trying to process {reserveName}", reserveName);
            }
        }

        private async void SafeDelete(string path)
        {
            var count = 0;
            var success = false;
            while (!success && count < 10)
            {
                try
                {
                    Directory.Delete(path, recursive: true);
                    success = true;
                }
                catch
                {
                    await Task.Delay(1000);
                    count++;
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
