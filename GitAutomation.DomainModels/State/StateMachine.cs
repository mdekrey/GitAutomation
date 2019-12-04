using GitAutomation.DomainModels;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace GitAutomation.State
{
    public class StateMachine<T> : IDispatcher, IStateMachine<T>
    {
        private readonly BehaviorSubject<StateUpdateEvent<T>> state;
        private readonly Func<T, IStandardAction, T> reducer;

        public T State => state.Value.Payload;
        public IAgentSpecification LastChangeBy => state.Value.LastChangeBy;
        public IObservable<StateUpdateEvent<T>> StateUpdates => state.AsObservable();

        public StateMachine(Func<T, IStandardAction, T> reducer, T zeroState)
        {
            this.state = new BehaviorSubject<StateUpdateEvent<T>>(new StateUpdateEvent<T>(zeroState, SystemAgent.Instance, "Zero state"));
            this.reducer = reducer;
        }

        public void Dispatch(StateUpdateEvent<IStandardAction> ev)
        {
            lock (state)
            {
                var original = state.Value;
                // TODO - authorization
                var newState = reducer(original.Payload, ev.Payload);
                state.OnNext(ev.WithState(newState));
            }
        }

    }
}
