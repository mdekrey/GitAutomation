using GitAutomation.DomainModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Web
{
    /// <summary>
    /// An agent that represents an anonymous user
    /// </summary>
    public class AnonymousUserAgent : IAgentSpecification
    {
        public static readonly IAgentSpecification Instance = new AnonymousUserAgent();
        private AnonymousUserAgent() { }
    }
}
