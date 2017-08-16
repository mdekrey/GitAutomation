using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Work
{
    class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IServiceProvider serviceProvider;

        public UnitOfWorkFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IUnitOfWork CreateUnitOfWork()
        {
            return new UnitOfWork(serviceProvider);
        }
    }
}
