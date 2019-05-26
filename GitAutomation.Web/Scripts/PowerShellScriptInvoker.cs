using GitAutomation.DomainModels;
using GitAutomation.Extensions;
using GitAutomation.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;

namespace GitAutomation.Web.Scripts
{
    public class PowerShellScriptInvoker
    {
        private readonly IDispatcher dispatcher;
        private readonly ILogger logger;
        private readonly string customPath;

        public PowerShellScriptInvoker(IDispatcher dispatcher, ILogger<PowerShellScriptInvoker> logger, IOptions<ConfigRepositoryOptions> options)
        {
            this.dispatcher = dispatcher;
            this.logger = logger;
            customPath = options.Value.CheckoutPath;
        }

        public IPowerShellStreams<PowerShellLine> Invoke(string scriptPath, object loggedParameters, object hiddenParameters, IAgentSpecification agentSpecification)
        {
            var invocationId = Guid.NewGuid();
            logger.LogInformation($"Invoking {scriptPath} ({invocationId}) with {JsonConvert.SerializeObject(loggedParameters)} as {agentSpecification}");
            var result = PowerShell.Create()
                .AddUnrestrictedCommand("./Scripts/Globals.ps1")
                .AddUnrestrictedCommand(ResolveScriptPath(scriptPath))
                .BindParametersToPowerShell(hiddenParameters)
                .BindParametersToPowerShell(loggedParameters)
                .InvokeAllStreams<PowerShellLine, string>(ps => JsonConvert.DeserializeObject<PowerShellLine>(ps));

            Task.Run(() => ProcessError(result, invocationId));
            Task.Run(() => ProcessActions(result, agentSpecification, invocationId));

            return result;
        }

        private async Task ProcessError(IPowerShellStreams<PowerShellLine> result, Guid invocationId)
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

        private async Task ProcessActions(IPowerShellStreams<PowerShellLine> result, IAgentSpecification agentSpecification, Guid invocationId)
        {
            await foreach (var line in result.SuccessAsync)
            {
                if (line.Action != null)
                {
                    logger.LogDebug($"Dispatching script({invocationId}) action {JsonConvert.SerializeObject(line.Action)}");
                    dispatcher.Dispatch(new StateUpdateEvent<StandardAction>(line.Action.Value, agentSpecification, line.Comment));
                }
            }
        }

        private string ResolveScriptPath(string scriptPath)
        {
            if (scriptPath.Contains("/.") || scriptPath.Contains("\\"))
            {
                throw new InvalidOperationException($"Invalid path found: {scriptPath}");
            }
            if (scriptPath.StartsWith("$/"))
            {
                return "./Scripts" + scriptPath.Substring(1);
            }
            // This could be arbitrary script execution if someone compromises a git repo. Don't have this on by default.
            //else if (scriptPath.StartsWith("./"))
            //{
            //    return Path.Combine(customPath, scriptPath.Substring(2));
            //}
            else
            {
                throw new NotImplementedException($"Unknown script path {scriptPath}");
            }
        }
    }
}
