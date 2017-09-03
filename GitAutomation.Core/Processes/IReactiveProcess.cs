﻿using System;
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
        StartInfo,
        Out,
        Error,
        ExitCode,
    }

    public struct OutputMessage
    {
        public OutputChannel Channel;
        public string Message;
        public int ExitCode;
    }

    public interface IReactiveProcess
    {
        IObservable<OutputMessage> Output { get; }
    }
}
