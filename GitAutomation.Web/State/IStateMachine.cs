using System;

namespace GitAutomation.Web.State
{
    public interface IStateMachine
    {
        RepositoryConfigurationState State { get; }
        IObservable<RepositoryConfigurationState> StateUpdates { get; }
    }
}