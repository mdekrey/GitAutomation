using GitAutomation.DomainModels;

namespace GitAutomation.State
{
    public class StateUpdateEvent<T>
    {
        public StateUpdateEvent(T state, IAgentSpecification lastChangeBy, string comment)
        {
            State = state;
            LastChangeBy = lastChangeBy;
            Comment = comment;
        }

        public T State { get; }
        public IAgentSpecification LastChangeBy { get; }
        public string Comment { get; private set; }

        public StateUpdateEvent<U> WithState<U>(U newState)
        {
            return new StateUpdateEvent<U>(newState, LastChangeBy, Comment);
        }
    }
}