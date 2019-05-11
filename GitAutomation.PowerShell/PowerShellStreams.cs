using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;

namespace GitAutomation
{
    public class PowerShellStreams<T>
    {
        private PowerShell psInstance;
        private readonly Task scriptCompletion;
        private readonly PSDataCollection<PSObject> input;
        private readonly PSDataCollection<T> output;
        private ImmutableList<ErrorRecord> persistedError;
        private ImmutableList<DebugRecord> persistedDebug;
        private ImmutableList<InformationRecord> persistedInformation;
        private ImmutableList<ProgressRecord> persistedProgress;
        private ImmutableList<VerboseRecord> persistedVerbose;
        private ImmutableList<WarningRecord> persistedWarning;

        public PowerShellStreams(PowerShell psInstance, Task scriptCompletion, PSDataCollection<PSObject> input, PSDataCollection<T> output, bool disposePowerShell)
        {
            this.psInstance = psInstance;
            this.scriptCompletion = scriptCompletion
                .ContinueWith(_ =>
                {
                    persistedError = psInstance.Streams.Error.ToImmutableList();
                    persistedDebug = psInstance.Streams.Debug.ToImmutableList();
                    persistedInformation = psInstance.Streams.Information.ToImmutableList();
                    persistedProgress = psInstance.Streams.Progress.ToImmutableList();
                    persistedVerbose = psInstance.Streams.Verbose.ToImmutableList();
                    persistedWarning = psInstance.Streams.Warning.ToImmutableList();
                })
                .ContinueWith(_ => { if (disposePowerShell) { psInstance.Dispose(); } })
                .ContinueWith(_ => this.psInstance = null);
            this.input = input;
            this.output = output;
        }

        public PSDataCollection<PSObject> Input => input;
        public Task Completion => scriptCompletion;

        public ImmutableList<T> Success => output.ToImmutableList();
        public ImmutableList<ErrorRecord> Error => persistedError ?? psInstance.Streams.Error.ToImmutableList();
        public ImmutableList<DebugRecord> Debug => persistedDebug ?? psInstance.Streams.Debug.ToImmutableList();
        public ImmutableList<InformationRecord> Information => persistedInformation ?? psInstance.Streams.Information.ToImmutableList();
        public ImmutableList<ProgressRecord> Progress => persistedProgress ?? psInstance.Streams.Progress.ToImmutableList();
        public ImmutableList<VerboseRecord> Verbose => persistedVerbose ?? psInstance.Streams.Verbose.ToImmutableList();
        public ImmutableList<WarningRecord> Warning => persistedWarning ?? psInstance.Streams.Warning.ToImmutableList();


        public IAsyncEnumerable<T> SuccessAsync => AsAsync(output, scriptCompletion);
        public IAsyncEnumerable<ErrorRecord> ErrorAsync => persistedError?.AsAsyncEnumerable() ?? AsAsync(psInstance.Streams.Error, scriptCompletion);
        public IAsyncEnumerable<DebugRecord> DebugAsync => persistedDebug?.AsAsyncEnumerable() ?? AsAsync(psInstance.Streams.Debug, scriptCompletion);
        public IAsyncEnumerable<InformationRecord> InformationAsync => persistedInformation?.AsAsyncEnumerable() ?? AsAsync(psInstance.Streams.Information, scriptCompletion);
        public IAsyncEnumerable<ProgressRecord> ProgressAsync => persistedProgress?.AsAsyncEnumerable() ?? AsAsync(psInstance.Streams.Progress, scriptCompletion);
        public IAsyncEnumerable<VerboseRecord> VerboseAsync => persistedVerbose?.AsAsyncEnumerable() ?? AsAsync(psInstance.Streams.Verbose, scriptCompletion);
        public IAsyncEnumerable<WarningRecord> WarningAsync => persistedWarning?.AsAsyncEnumerable() ?? AsAsync(psInstance.Streams.Warning, scriptCompletion);

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
