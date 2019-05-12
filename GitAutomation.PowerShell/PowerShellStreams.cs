using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GitAutomation
{
    public class PowerShellStreams<T>
    {
        private readonly PSDataCollection<T> output;
        private PowerShell psInstance;
        // The following get set when the above gets unset
        private ImmutableList<ErrorRecord> persistedError;
        private ImmutableList<DebugRecord> persistedDebug;
        private ImmutableList<InformationRecord> persistedInformation;
        private ImmutableList<ProgressRecord> persistedProgress;
        private ImmutableList<VerboseRecord> persistedVerbose;
        private ImmutableList<WarningRecord> persistedWarning;

        public PowerShellStreams(PowerShell psInstance, Task scriptCompletion, PSDataCollection<PSObject> input, PSDataCollection<T> output, bool disposePowerShell)
        {
            this.output = output;
            this.psInstance = psInstance;
            this.Parameters = (from command in psInstance.Commands.Commands
                               from parameter in command.Parameters
                               select parameter).ToImmutableList();
            this.Completion = scriptCompletion
                .ContinueWith(t =>
                {
                    this.Exception = t.Exception;
                    persistedError = psInstance.Streams.Error.ToImmutableList();
                    persistedDebug = psInstance.Streams.Debug.ToImmutableList();
                    persistedInformation = psInstance.Streams.Information.ToImmutableList();
                    persistedProgress = psInstance.Streams.Progress.ToImmutableList();
                    persistedVerbose = psInstance.Streams.Verbose.ToImmutableList();
                    persistedWarning = psInstance.Streams.Warning.ToImmutableList();
                })
                .ContinueWith(_ => { if (disposePowerShell) { psInstance.Dispose(); } })
                .ContinueWith(_ => this.psInstance = null);
            this.Input = input;
        }

        public ImmutableList<CommandParameter> Parameters { get; }
        public PSDataCollection<PSObject> Input { get; }
        public Task Completion { get; }
        public TaskAwaiter<PowerShellStreams<T>> GetAwaiter() => Completion.ContinueWith(_ => this).GetAwaiter();

        public ImmutableList<T> Success => output.ToImmutableList();
        public ImmutableList<ErrorRecord> Error => persistedError ?? psInstance.Streams.Error.ToImmutableList();
        public ImmutableList<DebugRecord> Debug => persistedDebug ?? psInstance.Streams.Debug.ToImmutableList();
        public ImmutableList<InformationRecord> Information => persistedInformation ?? psInstance.Streams.Information.ToImmutableList();
        public ImmutableList<ProgressRecord> Progress => persistedProgress ?? psInstance.Streams.Progress.ToImmutableList();
        public ImmutableList<VerboseRecord> Verbose => persistedVerbose ?? psInstance.Streams.Verbose.ToImmutableList();
        public ImmutableList<WarningRecord> Warning => persistedWarning ?? psInstance.Streams.Warning.ToImmutableList();


        public IAsyncEnumerable<T> SuccessAsync => AsAsync(output, Completion);
        public IAsyncEnumerable<ErrorRecord> ErrorAsync => persistedError?.AsAsyncEnumerable() ?? AsAsync(psInstance.Streams.Error, Completion);
        public IAsyncEnumerable<DebugRecord> DebugAsync => persistedDebug?.AsAsyncEnumerable() ?? AsAsync(psInstance.Streams.Debug, Completion);
        public IAsyncEnumerable<InformationRecord> InformationAsync => persistedInformation?.AsAsyncEnumerable() ?? AsAsync(psInstance.Streams.Information, Completion);
        public IAsyncEnumerable<ProgressRecord> ProgressAsync => persistedProgress?.AsAsyncEnumerable() ?? AsAsync(psInstance.Streams.Progress, Completion);
        public IAsyncEnumerable<VerboseRecord> VerboseAsync => persistedVerbose?.AsAsyncEnumerable() ?? AsAsync(psInstance.Streams.Verbose, Completion);
        public IAsyncEnumerable<WarningRecord> WarningAsync => persistedWarning?.AsAsyncEnumerable() ?? AsAsync(psInstance.Streams.Warning, Completion);

        public AggregateException Exception { get; private set; }

        private static async IAsyncEnumerable<U> AsAsync<U>(PSDataCollection<U> output, Task until)
        {
            var nextIndex = 0;

            while (nextIndex < output.Count || !until.IsCompleted)
            {
                if (nextIndex < output.Count)
                {
                    yield return output[nextIndex++];
                    continue;
                }

                var dataAddedEvent = new TaskCompletionSource<object>();
                void DataAdded(object sender, EventArgs e) { dataAddedEvent.SetResult(null); }
                output.DataAdded += DataAdded;

                await Task.WhenAny(until, dataAddedEvent.Task);

                output.DataAdded -= DataAdded;
            }

            yield break;
        }
    }
}
