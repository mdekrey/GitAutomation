using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Web
{
    public class AutomationOptions
    {
        public int WorkerCount { get; set; }
        public string WorkspacePath { get; set; }
        public string WorkingRemote { get; set; }
        public string DefaultRemote { get; set; }
        public string IntegrationPrefix { get; set; }
    }
}
