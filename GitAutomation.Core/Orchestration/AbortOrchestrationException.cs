using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Orchestration
{
    public class AbortOrchestrationException : Exception
    {
        public AbortOrchestrationException(string message) : base(message)
        {
        }

        public AbortOrchestrationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
