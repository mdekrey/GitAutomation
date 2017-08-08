using System;
using System.Collections.Generic;
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

    public struct OutputMessage
    {
        public OutputChannel Channel;
        public string Message;
    }

    public interface IReactiveProcess
    {
        IObservable<Unit> ProcessExited { get; }
        IObservable<OutputMessage> Output { get; }
    }
}
