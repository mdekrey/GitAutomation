using GitAutomation.State;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.DomainModels
{
    public interface IDispatcher
    {
        void Dispatch(StateUpdateEvent<IStandardAction> ev);
    }

    public static class DispatcherExtensions
    {
        public static void Dispatch(this IDispatcher dispatcher, IStandardAction action, IAgentSpecification lastChangeBy, string comment) =>
            dispatcher.Dispatch(new StateUpdateEvent<IStandardAction>(action, lastChangeBy, comment));
    }
}
