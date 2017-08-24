using System;

namespace GitAutomation.Repository
{
    public interface IRepositoryStateDriver : IDisposable
    {
        void Start();
    }
}