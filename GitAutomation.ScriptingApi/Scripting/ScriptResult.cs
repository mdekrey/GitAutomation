using System;
using System.Collections.Immutable;

namespace GitAutomation.Scripting
{
    public class ScriptResult
    {
        public ScriptResult(AggregateException? exception, ImmutableList<LogMessage> logs, object? inputs)
        {
            Exception = exception;
            Logs = logs;
            Inputs = inputs;
        }

        public AggregateException? Exception { get; }
        public ImmutableList<LogMessage> Logs { get; }
        public object? Inputs { get; }
    }
}
