using System;

namespace GitAutomation.Web.State
{
    public interface IStateMachine<T>
    {
        T State { get; }
        IObservable<T> StateUpdates { get; }
    }
}