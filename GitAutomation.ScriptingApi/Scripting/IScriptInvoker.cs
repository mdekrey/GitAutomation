using GitAutomation.DomainModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Scripting
{
    public interface IScriptInvoker
    {
        ScriptProgress Invoke<TParams>(Type script, TParams loggedParameters, IAgentSpecification agentSpecification); // where script : IScript<TParams>;

        Type GetScript<TParams>(string scriptName);
    }
}
