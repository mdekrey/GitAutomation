using GitAutomation.Processes;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Orchestration
{
    public interface IUniqueAction : IRepositoryAction
    {
        void AbortAs(IObservable<OutputMessage> otherStream);
    }
}
