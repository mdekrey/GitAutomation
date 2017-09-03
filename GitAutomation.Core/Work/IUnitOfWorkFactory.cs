using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Work
{
    public interface IUnitOfWorkFactory
    {
        IUnitOfWork CreateUnitOfWork();
    }
}
