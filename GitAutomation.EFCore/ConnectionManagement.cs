using GitAutomation.Work;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.EFCore
{
    public class ConnectionManagement<T> : IUnitOfWorkLifecycleManagement
        where T : DbContext
    {
        private readonly T context;

        public ConnectionManagement(T context)
        {
            this.context = context;
        }

        public Task Commit()
        {
            return context.SaveChangesAsync();
        }

        public Task Prepare()
        {
            return Task.CompletedTask;
        }

        public Task Rollback()
        {
            return Task.CompletedTask;
        }
    }
}
