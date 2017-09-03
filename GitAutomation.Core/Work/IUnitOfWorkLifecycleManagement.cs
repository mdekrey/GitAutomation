using System;
using System.Threading.Tasks;

namespace GitAutomation.Work
{
    public interface IUnitOfWorkLifecycleManagement
    {
        Task Prepare();
        Task Commit();
        Task Rollback();
    }
}