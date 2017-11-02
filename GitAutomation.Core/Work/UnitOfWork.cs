using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitAutomation.Work
{
    internal class UnitOfWork : IUnitOfWork
    {
        struct PortionOfWork
        {
            public Func<IServiceProvider, Task> Action;
        }

        private readonly List<PortionOfWork> work = new List<PortionOfWork>();
        private readonly ConcurrentDictionary<Type, Type> finalize = new ConcurrentDictionary<Type, Type>();
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

        public void PrepareAndFinalize<T>()
            where T : IUnitOfWorkLifecycleManagement
        {
            finalize.GetOrAdd(typeof(T), typeof(T));
        }

        public async Task CommitAsync()
        {
            using (var scope = provider.CreateScope())
            {
                var values = finalize.Values.Select(type => scope.ServiceProvider.GetRequiredService(type) as IUnitOfWorkLifecycleManagement).ToArray();
                try
                {
                    foreach (var portionOfWork in values)
                    {
                        await portionOfWork.Prepare();
                    }

                    foreach (var portionOfWork in work)
                    {
                        await portionOfWork.Action(scope.ServiceProvider);
                    }

                    foreach (var portionOfWork in values)
                    {
                        await portionOfWork.Commit();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    foreach (var portionOfWork in values)
                    {
                        await portionOfWork.Rollback();
                    }
                    throw;
                }
            }
        }

        void IDisposable.Dispose()
        {
        }
    }
}