using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitAutomation.DomainModels
{
    public class CompositeDispatcher : IDispatcher
    {
        private ImmutableList<IDispatcher> dispatchers = ImmutableList<IDispatcher>.Empty;

        public event EventHandler Dispatched;

        public void AddDispatcher(IDispatcher dispatcher)
        {
            dispatchers = dispatchers.Add(dispatcher);
        }


        public void Dispatch(StandardAction action)
        {
            foreach (var dispatcher in dispatchers)
            {
                dispatcher.Dispatch(action);
            }

            Dispatched?.Invoke(this, EventArgs.Empty);
        }
    }
}
