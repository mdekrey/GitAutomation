using GitAutomation.DomainModels;
using GitAutomation.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;

namespace GitAutomation.Web.Scripts
{
    public class PowerShellScriptInvoker
    {
        private readonly IDispatcher dispatcher;
        private readonly ILogger logger;

        public PowerShellScriptInvoker(IDispatcher dispatcher, ILogger<PowerShellScriptInvoker> logger)
        {
            this.dispatcher = dispatcher;
            this.logger = logger;
        }

        public IPowerShellStreams<StandardAction> Invoke(string scriptPath, object loggedParameters, object hiddenParameters, IAgentSpecification agentSpecification)
        {
            var invocationId = Guid.NewGuid();
            logger.LogInformation($"Invoking {scriptPath} ({invocationId}) with {JsonConvert.SerializeObject(loggedParameters)} as {agentSpecification}");
            var result = PowerShell.Create()
                .AddUnrestrictedCommand("./Scripts/Globals.ps1")
                .AddUnrestrictedCommand(ResolveScriptPath(scriptPath))
                .BindParametersToPowerShell(hiddenParameters)
                .BindParametersToPowerShell(loggedParameters)
                .InvokeAllStreams<StandardAction, string>(ps => JsonConvert.DeserializeObject<StandardAction>(ps));

            Task.Run(() => ProcessError(result, invocationId));
            Task.Run(() => ProcessActions(result, agentSpecification, invocationId));

            return result;
        }

        private async Task ProcessError(IPowerShellStreams<StandardAction> result, Guid invocationId)
        {
            await foreach (var errorRecord in result.ErrorAsync)
            {
                if (errorRecord.FullyQualifiedErrorId == "NativeCommandError" || errorRecord.FullyQualifiedErrorId == "NativeCommandErrorMessage")
                {
                    logger.LogDebug($"Script({invocationId}) provided output on the error stream: ${errorRecord.Exception.Message}\n${errorRecord.ScriptStackTrace}");
                }
                else
                {
                    logger.LogError(errorRecord.Exception, $"Script({invocationId}) encountered an exception at ${errorRecord.ScriptStackTrace}");
                }
            }
        }

        private async Task ProcessActions(IPowerShellStreams<StandardAction> result, IAgentSpecification agentSpecification, Guid invocationId)
        {
            await foreach (var action in result.SuccessAsync)
            {
                logger.LogDebug($"Dispatching script({invocationId}) action {JsonConvert.SerializeObject(action)}");
                dispatcher.Dispatch(action, agentSpecification);
            }
        }

        private string ResolveScriptPath(string scriptPath)
        {
            if (scriptPath.StartsWith("$/"))
            {
                return "./Scripts" + scriptPath.Substring(1);
            }
            else
            {
                throw new NotImplementedException($"Unknown script path {scriptPath}");
            }
        }
    }
}
