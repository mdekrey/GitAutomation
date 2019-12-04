using GitAutomation.DomainModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Scripting
{
    public interface IScript<in TParams>
    {
        Task Run(TParams parameters, ILogger logger, IAgentSpecification agent);
    }
}
