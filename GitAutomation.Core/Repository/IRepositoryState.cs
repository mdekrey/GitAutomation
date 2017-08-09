using System;

namespace GitAutomation.Repository
{
    public interface IRepositoryState
    {
        IObservable<Processes.OutputMessage> Initialize();
        IObservable<string> Reset();
        IObservable<string[]> RemoteBranches();
    }
}