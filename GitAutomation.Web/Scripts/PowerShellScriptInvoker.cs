using GitAutomation.DomainModels;
using GitAutomation.Extensions;
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

        class PowerShellStandardAction
        {
            public string Action { get; set; }
            public Hashtable Payload { get; set; }

            public StandardAction ToStandardAction()
            {
                return new StandardAction(Action, Payload.Keys.Cast<string>().ToDictionary(k => k, k => Payload[k]));
            }
        }

        public PowerShellScriptInvoker(IDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public IPowerShellStreams<StandardAction> Invoke(string scriptPath, object loggedParameters, object hiddenParameters, IAgentSpecification agentSpecification)
        {
            var result = PowerShell.Create()
                .AddUnrestrictedCommand("./Scripts/Globals.ps1")
                .AddUnrestrictedCommand(ResolveScriptPath(scriptPath))
                .BindParametersToPowerShell(hiddenParameters)
                .BindParametersToPowerShell(loggedParameters)
                .InvokeAllStreams<StandardAction, PowerShellStandardAction>(ps => ps.ToStandardAction());

            Task.Run(() => ProcessActions(result, agentSpecification));

            return result;
        }

        private async Task ProcessActions(IPowerShellStreams<StandardAction> result, IAgentSpecification agentSpecification)
        {
            await foreach (var action in result.SuccessAsync)
            {
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
