using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GitAutomation.Processes
{
    public interface IReactiveProcessFactory
    {
        IReactiveProcess BuildProcess(ProcessStartInfo startInfo);
    }
}
