using System;
using System.Collections.Generic;
using System.Text;
using GitAutomation.DomainModels;

namespace GitAutomation.Scripting
{
    public class ScriptInvoker : IScriptInvoker
    {
        public ScriptProgress Invoke(string scriptPath, object loggedParameters, object hiddenParameters, IAgentSpecification agentSpecification)
        {
            throw new NotImplementedException();
        }
    }
}
