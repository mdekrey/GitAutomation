using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;

namespace GitAutomation.Processes
{
    class MultiReactiveProcess : IReactiveProcess
    {
        public MultiReactiveProcess(params IReactiveProcess[] processes)
        {
            Output = Observable.Concat(processes.Select(process => process.Output).ToArray());
        }


        public IObservable<OutputMessage> Output { get; }
    }
}
