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
            return process.Output.FirstOutputMessage();
        }

        public static IObservable<string> FirstErrorMessage(this IReactiveProcess process)
        {
            return process.Output.FirstErrorMessage();
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
