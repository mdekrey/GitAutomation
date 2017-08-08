using System;

namespace GitAutomation.Repository
{
    public interface IRepositoryState
    {
        IObservable<string> Initialize();
        IObservable<string> Reset();
    }
}