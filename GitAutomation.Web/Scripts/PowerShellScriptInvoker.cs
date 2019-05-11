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
        public async Task<PowerShellStreams<StandardAction>> Invoke(string scriptPath, object loggedParameters, object hiddenParameters)
        {
            var result = PowerShell.Create()
                .AddUnrestrictedCommand("./Scripts/Globals.ps1")
                .AddUnrestrictedCommand(ResolveScriptPath(scriptPath))
                .BindParametersToPowerShell(hiddenParameters)
                .BindParametersToPowerShell(loggedParameters)
                .InvokeAllStreams<StandardAction>();
            await result.Completion;
            return result;
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
