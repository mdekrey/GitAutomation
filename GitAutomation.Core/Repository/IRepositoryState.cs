using GitAutomation.Processes;
using System;

namespace GitAutomation.Repository
{
    public interface IRepositoryState
    {
        IObservable<OutputMessage> ProcessActions();

        IObservable<OutputMessage> Reset();
        IObservable<OutputMessage> CheckForUpdates();

        IObservable<string[]> RemoteBranches();
    }
}