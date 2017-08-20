using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Work
{
    public interface IUnitOfWork : IDisposable
    {
        Task CommitAsync();

        void Defer(Func<IServiceProvider, Task> action);

        /// <summary>
        /// Prepares a finalization action based on a key
        /// </summary>
        /// <param name="key">Key for the finalize action</param>
        /// <param name="prepareAction">The action to run before the deferred actions are committed.</param>
        /// <param name="commitAction">The action to run after the deferred actions are committed.</param>
        /// <param name="rollbackAction">The action to run upon rollback.</param>
        /// <returns>True if the finalization step was added, otherwise false, which means it was already added.</returns>
        bool PrepareAndFinalize(string key, Func<IServiceProvider, Task> prepareAction, Func<IServiceProvider, Task> commitAction, Func<IServiceProvider, Task> rollbackAction);
    }
}
