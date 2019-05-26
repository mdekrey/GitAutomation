using GitAutomation.DomainModels;

namespace GitAutomation.State
{
    public class StateUpdateEvent<T>
    {
        public StateUpdateEvent(T state, IAgentSpecification lastChangeBy)
        {
            State = state;
            LastChangeBy = lastChangeBy;
        }

        public T State { get; }
        public IAgentSpecification LastChangeBy { get; }

        public StateUpdateEvent<U> WithState<U>(U newState)
        {
            return new StateUpdateEvent<U>(newState, LastChangeBy);
        }
    }
}