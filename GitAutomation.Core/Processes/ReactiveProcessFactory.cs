using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GitAutomation.Processes
{
    public class ReactiveProcessFactory : IReactiveProcessFactory
    {
        public IReactiveProcess BuildProcess(ProcessStartInfo startInfo)
        {
            return new ReactiveProcess(startInfo);
        }
    }
}
