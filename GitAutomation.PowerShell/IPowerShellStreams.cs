using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GitAutomation
{
    public interface IPowerShellStreams<T>
    {
        Task Completion { get; }
        ImmutableList<DebugRecord> Debug { get; }
        IAsyncEnumerable<DebugRecord> DebugAsync { get; }
        ImmutableList<ErrorRecord> Error { get; }
        IAsyncEnumerable<ErrorRecord> ErrorAsync { get; }
        AggregateException Exception { get; }
        ImmutableList<InformationRecord> Information { get; }
        IAsyncEnumerable<InformationRecord> InformationAsync { get; }
        PSDataCollection<PSObject> Input { get; }
        ImmutableList<CommandParameter> Parameters { get; }
        ImmutableList<ProgressRecord> Progress { get; }
        IAsyncEnumerable<ProgressRecord> ProgressAsync { get; }
        ImmutableList<T> Success { get; }
        IAsyncEnumerable<T> SuccessAsync { get; }
        ImmutableList<VerboseRecord> Verbose { get; }
        IAsyncEnumerable<VerboseRecord> VerboseAsync { get; }
        ImmutableList<WarningRecord> Warning { get; }
        IAsyncEnumerable<WarningRecord> WarningAsync { get; }

        TaskAwaiter<IPowerShellStreams<T>> GetAwaiter();
    }
}