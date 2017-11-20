using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Processes
{
    public enum OutputChannel
    {
        Out,
        Error,
    }

    public enum ProcessState
    {
        NotStarted,
        Running,
        Exited,
        Cancelled
    }

    public struct OutputMessage
    {
        public OutputChannel Channel;
        public string Message;
    }

    public interface IReactiveProcess
    {
        string StartInfo { get; }

        ProcessState State { get; }

        /// <summary>
        /// A cold observable. If subscribed and the process is not started, the process will start. Outputs immediately
        /// the current state of the process when subscribed to, and any other changes.
        /// </summary>
        IObservable<ProcessState> ActiveState { get; }

        /// <summary>
        /// Only valid after the process state is "Exited".
        /// </summary>
        int ExitCode { get; }

        ImmutableList<OutputMessage> Output { get; }

        IObservable<OutputMessage> ActiveOutput { get; }
    }
}
