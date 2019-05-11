using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;

namespace GitAutomation.Extensions
{
    public static class PowerShellExtensions
    {
        public static PowerShell BindParametersToPowerShell<T>(this PowerShell instance, T parameters)
        {
            var dictionary = parameters.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(parameters));
            instance.AddParameters(dictionary);
            return instance;
        }

        public static PowerShell AddUnrestrictedCommand(this PowerShell instance, string scriptName)
        {
            return instance.AddScript(File.ReadAllText(scriptName));
        }

        public static PowerShell AddUnrestrictedCommand(this PowerShell instance, string scriptName, bool useLocalScope)
        {
            return instance.AddScript(File.ReadAllText(scriptName), useLocalScope);
        }

        public static PowerShellStreams<T> InvokeAllStreams<T>(this PowerShell psInstance, bool disposePowerShell = true)
        {
            var input = new PSDataCollection<PSObject>();
            var output = new PSDataCollection<T>();
            var scriptCompletion = Task.Factory.FromAsync(psInstance.BeginInvoke(input, output), psInstance.EndInvoke);

            return new PowerShellStreams<T>(psInstance, scriptCompletion, input, output, disposePowerShell);
        }

    }
}
