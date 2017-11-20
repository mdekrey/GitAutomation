using System;
using System.Collections.Generic;
using System.Text;
using System.Reactive.Linq;

namespace GitAutomation.Processes
{
    public static class ReactiveProcessExtensions
    {
        public static IObservable<string> GetFirstOutput(this IReactiveProcess target)
        {
            return (from o in target.ActiveOutput
                    where o.Channel == OutputChannel.Out
                    select o.Message).FirstOrDefaultAsync();
        }
    }
}
