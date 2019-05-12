using GitAutomation.DomainModels;
using GitAutomation.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;

namespace GitAutomation.Web.Scripts
{
    public class PowerShellScriptInvoker
    {
        private readonly IDispatcher dispatcher;

        public PowerShellScriptInvoker(IDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public PowerShellStreams<StandardAction> Invoke(string scriptPath, object loggedParameters, object hiddenParameters)
        {
            var result = PowerShell.Create()
                .AddUnrestrictedCommand("./Scripts/Globals.ps1")
                .AddUnrestrictedCommand(ResolveScriptPath(scriptPath))
                .BindParametersToPowerShell(hiddenParameters)
                .BindParametersToPowerShell(loggedParameters)
                .InvokeAllStreams<StandardAction>();

            Task.Run(() => ProcessActions(result));

            return result;
        }

        private async Task ProcessActions(PowerShellStreams<StandardAction> result)
        {
            await foreach (var action in result.SuccessAsync)
            {
                dispatcher.Dispatch(action);
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
