using GitAutomation.Processes;
using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Orchestration
{
    public interface IRepositoryActionEntry
    {
        Task WaitUntilComplete();
    }
    
    public class StaticRepositoryActionEntry : IRepositoryActionEntry
    {
        public string Message { get; }
        public bool IsError { get; }

        public StaticRepositoryActionEntry(string message, bool isError = false)
        {
            Message = message;
            IsError = isError;
        }

        public Task WaitUntilComplete()
        {
            return Task.CompletedTask;
        }

        public System.Runtime.CompilerServices.TaskAwaiter<StaticRepositoryActionEntry> GetAwaiter()
        {
            return WaitUntilComplete().ContinueWith(t => this).GetAwaiter();
        }
    }

    public class RepositoryActionReactiveProcessEntry : IRepositoryActionEntry
    {
        private readonly IReactiveProcess process;

        public RepositoryActionReactiveProcessEntry(IReactiveProcess process)
        {
            this.process = process;
        }

        public IReactiveProcess Process => process;

        public Task WaitUntilComplete()
        {
            return process.ActiveState.ToTask();
        }

        public System.Runtime.CompilerServices.TaskAwaiter<RepositoryActionReactiveProcessEntry> GetAwaiter()
        {
            return WaitUntilComplete().ContinueWith(t => this).GetAwaiter();
        }
    }
}
