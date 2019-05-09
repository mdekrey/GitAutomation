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

        public static async IAsyncEnumerable<T> InvokeEnumerableAsync<T>(this PowerShell psInstance)
        {
            var collection = new PSDataCollection<T>();
            var completionEvent = Task.Factory.FromAsync(psInstance.BeginInvoke(new PSDataCollection<PSObject>(), collection), psInstance.EndInvoke);

            var nextIndex = 0;

            while (nextIndex < collection.Count || !completionEvent.IsCompleted)
            {
                if (nextIndex < collection.Count)
                {
                    yield return collection[nextIndex++];
                    continue;
                }

                var dataAddedEvent = new TaskCompletionSource<object>();
                void DataAdded(object sender, EventArgs e) { dataAddedEvent.SetResult(null); }
                collection.DataAdded += DataAdded;

                await Task.WhenAny(completionEvent, dataAddedEvent.Task);

                collection.DataAdded -= DataAdded;
            }

            yield break;
        }
    }
}
