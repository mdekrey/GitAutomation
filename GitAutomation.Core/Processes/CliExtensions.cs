using System;
using System.Collections.Generic;
using System.Text;
using System.Reactive.Linq;

namespace GitAutomation.Processes
{
    public static class CliExtensions
    {
        public static IObservable<string> FirstOutputMessage(this IReactiveProcess process)
        {
            return process.ActiveOutput.FirstOutputMessage();
        }

        public static IObservable<string> FirstErrorMessage(this IReactiveProcess process)
        {
            return process.ActiveOutput.FirstErrorMessage();
        }

        public static IObservable<int> ExitCode(this IReactiveProcess process)
        {
            return process.ActiveState.IgnoreElements().Select(_ => 0).Concat(Observable.Return(0)).SelectMany(p => Observable.Return(process.ExitCode));
        }
        
        public static IObservable<string> FirstOutputMessage(this IObservable<OutputMessage> process)
        {
            return (from o in process where o.Channel == OutputChannel.Out select o.Message).FirstOrDefaultAsync();
        }

        public static IObservable<string> FirstErrorMessage(this IObservable<OutputMessage> process)
        {
            return (from o in process where o.Channel == OutputChannel.Error select o.Message).FirstOrDefaultAsync();
        }
    }
}
