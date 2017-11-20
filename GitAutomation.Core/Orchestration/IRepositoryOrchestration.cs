using GitAutomation.Processes;
using GitAutomation.Repository;
using System;
using System.Collections.Immutable;

namespace GitAutomation.Orchestration
{
    public interface IRepositoryOrchestration
    {
        IObservable<IRepositoryActionEntry> ProcessActions();
        IObservable<ImmutableList<IRepositoryActionEntry>> ProcessActionsLog { get; }
        IObservable<ImmutableList<IRepositoryAction>> ActionQueue { get; }

        IObservable<IRepositoryActionEntry> EnqueueAction(IRepositoryAction resetAction, bool skipDuplicateCheck = false);
    }
}
