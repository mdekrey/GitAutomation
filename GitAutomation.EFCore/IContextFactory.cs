using Microsoft.EntityFrameworkCore;

namespace GitAutomation.EFCore
{
    public interface IContextFactory<T> where T : DbContext
    {
        T GetContext();
    }
}