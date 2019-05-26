using GitAutomation.State;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.DomainModels
{
    public interface IDispatcher
    {
        void Dispatch(StateUpdateEvent<StandardAction> ev);
    }
}
