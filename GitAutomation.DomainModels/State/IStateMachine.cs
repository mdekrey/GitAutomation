using GitAutomation.DomainModels;
using System;

namespace GitAutomation.State
{
    public interface IStateMachine<T>
    {
        T State { get; }
        IAgentSpecification LastChangeBy { get; }
        IObservable<StateUpdateEvent<T>> StateUpdates { get; }
    }
}