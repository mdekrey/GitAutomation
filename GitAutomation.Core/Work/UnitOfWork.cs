using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitAutomation.Work
{
    internal class UnitOfWork : IUnitOfWork
    {
        struct PortionOfWork
        {
            public Func<IServiceProvider, Task> Action;
        }
        struct FinalizeWork
        {
            public Func<IServiceProvider, Task> Prepare;
            public Func<IServiceProvider, Task> Commit;
            public Func<IServiceProvider, Task> Rollback;
        }

        private readonly List<PortionOfWork> work = new List<PortionOfWork>();
        private readonly Dictionary<string, FinalizeWork> finalize = new Dictionary<string, FinalizeWork>();
        private readonly IServiceProvider provider;

        public UnitOfWork(IServiceProvider provider)
        {
            this.provider = provider;
        }

        public void Defer(Func<IServiceProvider, Task> action) =>
            work.Add(new PortionOfWork
            {
                Action = action,
            });

        public bool PrepareAndFinalize(string key, Func<IServiceProvider, Task> prepareAction, Func<IServiceProvider, Task> commitAction, Func<IServiceProvider, Task> rollbackAction)
        {
            if (finalize.ContainsKey(key))
            {
                return false;
            }
            finalize.Add(key, new FinalizeWork
            {
                Prepare = prepareAction,
                Commit = commitAction,
                Rollback = rollbackAction
            });
            return true;
        }

        public async Task CommitAsync()
        {
            using (var scope = provider.CreateScope())
            {
                try
                {
                    foreach (var portionOfWork in finalize.Values)
                    {
                        await portionOfWork.Commit?.Invoke(scope.ServiceProvider);
                    }

                    foreach (var portionOfWork in work)
                    {
                        await portionOfWork.Action(scope.ServiceProvider);
                    }

                    foreach (var portionOfWork in finalize.Values)
                    {
                        await portionOfWork.Commit?.Invoke(scope.ServiceProvider);
                    }
                }
                finally
                {
                    foreach (var portionOfWork in finalize.Values)
                    {
                        await portionOfWork.Rollback?.Invoke(scope.ServiceProvider);
                    }
                }
            }
        }

        void IDisposable.Dispose()
        {
        }
    }
}