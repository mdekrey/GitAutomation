using GitAutomation.Work;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace GitAutomation.EFCore
{
    class ContextFactory<T> : IContextFactory<T>
        where T : DbContext
    {
        private readonly Func<T> factory;

        public ContextFactory(Func<T> factory)
        {
            this.factory = factory;

            // Once, make sure our database is up-to-date
            factory().Database.Migrate();
        }

        public T GetContext() => factory();
    }
}