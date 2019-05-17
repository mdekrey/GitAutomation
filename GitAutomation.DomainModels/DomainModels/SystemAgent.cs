using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.DomainModels
{
    /// <summary>
    /// An agent that represents the GitAuto system; should not be used outside of fully automated, unconfigured processes
    /// </summary>
    public sealed class SystemAgent : IAgentSpecification
    {
        public static readonly IAgentSpecification Instance = new SystemAgent();
        private SystemAgent() { }
    }
}
