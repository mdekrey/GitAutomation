using GitAutomation.Processes;
using System;
using System.Collections.Immutable;

namespace GitAutomation.Repository
{
    public interface IRepositoryState
    {
        IObservable<OutputMessage> ProcessActions();
        IObservable<ImmutableList<OutputMessage>> ProcessActionsLog { get; }

        IObservable<OutputMessage> Reset();
        IObservable<OutputMessage> CheckForUpdates();

        IObservable<string[]> RemoteBranches();
    }
}