using GitAutomation.DomainModels;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace GitAutomation.Web.State
{
    public class StateMachine<T> : IDispatcher, IStateMachine<T>
    {
        private readonly BehaviorSubject<T> state;
        private readonly Func<T, StandardAction, T> reducer;

        public T State => state.Value;
        public IObservable<T> StateUpdates => state.AsObservable();

        public StateMachine(Func<T, StandardAction, T> reducer, T zeroState)
        {
            this.state = new BehaviorSubject<T>(zeroState);
            this.reducer = reducer;
        }

        public void Dispatch(StandardAction action)
        {
            lock (state)
            {
                var original = state.Value;
                state.OnNext(reducer(original, action));
            }
        }

    }
}
