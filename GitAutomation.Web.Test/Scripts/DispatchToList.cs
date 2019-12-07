using System.Collections.Generic;
using GitAutomation.DomainModels;
using GitAutomation.State;

namespace GitAutomation.Scripts
{
    internal class DispatchToList : IDispatcher
    {
        private List<StateUpdateEvent<IStandardAction>> resultList;

        public DispatchToList(List<StateUpdateEvent<IStandardAction>> resultList)
        {
            this.resultList = resultList;
        }

        public void Dispatch(StateUpdateEvent<IStandardAction> ev)
        {
            resultList.Add(ev);
        }
    }
}
