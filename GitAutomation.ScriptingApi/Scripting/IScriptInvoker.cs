using GitAutomation.DomainModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Scripting
{
    public interface IScriptInvoker
    {
        ScriptProgress Invoke(string scriptPath, object loggedParameters, object hiddenParameters, IAgentSpecification agentSpecification);
    }
}
