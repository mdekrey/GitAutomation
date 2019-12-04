using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Scripting
{
    public class ScriptProgress
    {
        public ScriptProgress(Task scriptCompletion)
        {
            // TODO - Inputs? Outputs?
            this.Completion = scriptCompletion
                .ContinueWith(t => new ScriptResult(
                    exception: t.Exception
                ));
        }

        public Task<ScriptResult> Completion { get; }
        public TaskAwaiter<ScriptResult> GetAwaiter() => Completion.GetAwaiter();

    }
}
