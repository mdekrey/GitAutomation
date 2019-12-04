using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Scripting
{
    public class ScriptResult
    {
        public ScriptResult(AggregateException? exception)
        {
            this.Exception = exception;
        }

        public AggregateException? Exception { get; }

    }
}
