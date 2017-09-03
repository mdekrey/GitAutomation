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

        void PrepareAndFinalize<T>()
            where T : IUnitOfWorkLifecycleManagement;
        
    }
}
