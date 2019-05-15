using GitAutomation.DomainModels;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace GitAutomation.Web.State
{
    public class StateMachine : IDispatcher, IStateMachine
    {
        private readonly BehaviorSubject<RepositoryConfigurationState> state = new BehaviorSubject<RepositoryConfigurationState>(RepositoryConfigurationState.ZeroState);

        public RepositoryConfigurationState State => state.Value;
        public IObservable<RepositoryConfigurationState> StateUpdates => state.AsObservable();

        public void Dispatch(StandardAction action)
        {
            lock (state)
            {
                var original = state.Value;
                state.OnNext(RepositoryConfigurationStateReducer.Reduce(original, action));
            }
        }

    }
}
