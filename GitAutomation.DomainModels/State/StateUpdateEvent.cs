using GitAutomation.DomainModels;

namespace GitAutomation.State
{
    public class StateUpdateEvent<T>
    {
        public StateUpdateEvent(T payload, IAgentSpecification lastChangeBy, string comment)
        {
            Payload = payload;
            LastChangeBy = lastChangeBy;
            Comment = comment;
        }

        public T Payload { get; }
        public IAgentSpecification LastChangeBy { get; }
        public string Comment { get; private set; }

        public StateUpdateEvent<U> WithState<U>(U newState)
        {
            return new StateUpdateEvent<U>(newState, LastChangeBy, Comment);
        }
    }
}