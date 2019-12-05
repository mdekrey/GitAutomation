using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Scripting
{
    public class ScriptProgress
    {
        public ScriptProgress(Task scriptCompletion, IEnumerable<LogMessage> logs, object? inputs)
        {
            Logs = logs;
            Inputs = inputs;
            Completion = scriptCompletion
                .ContinueWith(t => new ScriptResult(
                    inputs: Inputs,
                    logs: logs.ToImmutableList(),
                    exception: t.Exception
                ));
        }

        public IEnumerable<LogMessage> Logs { get; }
        public object? Inputs { get; }
        public Task<ScriptResult> Completion { get; }
        public TaskAwaiter<ScriptResult> GetAwaiter() => Completion.GetAwaiter();

    }
}
