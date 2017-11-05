using GitAutomation.Processes;
using GitAutomation.Repository;
using System;
using System.Collections.Immutable;

namespace GitAutomation.Orchestration
{
    public interface IRepositoryOrchestration
    {
        IObservable<OutputMessage> ProcessActions();
        IObservable<ImmutableList<OutputMessage>> ProcessActionsLog { get; }
        IObservable<ImmutableList<IRepositoryAction>> ActionQueue { get; }

        IObservable<OutputMessage> EnqueueAction(IRepositoryAction resetAction, bool skipDuplicateCheck = false);
    }
}
