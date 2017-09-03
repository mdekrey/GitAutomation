using System;

namespace GitAutomation.Orchestration
{
    public interface IRepositoryStateDriver : IDisposable
    {
        void Start();
    }
}