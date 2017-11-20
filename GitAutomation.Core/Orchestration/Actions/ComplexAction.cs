using GitAutomation.Processes;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace GitAutomation.Orchestration.Actions
{
    abstract class ComplexAction<T> : IRepositoryAction
        where T : ComplexActionInternal
    {
        protected readonly Subject<IRepositoryActionEntry> output = new Subject<IRepositoryActionEntry>();

        public abstract string ActionType { get; }

        public abstract JToken Parameters { get; }

        public IObservable<IRepositoryActionEntry> ProcessStream => output;

        public IObservable<IRepositoryActionEntry> PerformAction(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<T>(serviceProvider, parameters: GetExtraParameters()).Process().Multicast(output).RefCount();
        }

        internal virtual object[] GetExtraParameters() => Array.Empty<object>();
    }

    abstract class ComplexUniqueAction<T> : ComplexAction<T>, IUniqueAction
        where T : ComplexActionInternal
    {
        public void AbortAs(IObservable<IRepositoryActionEntry> otherStream)
        {
            otherStream.Multicast(output).Connect();
        }
    }

    internal abstract class ComplexActionInternal
    {
        private readonly IObservable<IRepositoryActionEntry> process;
        private readonly Subject<IRepositoryActionEntry> processes;

        public ComplexActionInternal()
        {
            var disposable = new CompositeDisposable();
            this.processes = new Subject<IRepositoryActionEntry>();
            disposable.Add(processes);

            this.process = Observable.Create<IRepositoryActionEntry>(async (observer, cancellationToken) =>
            {
                if (disposable.IsDisposed)
                {
                    observer.OnError(new ObjectDisposedException(nameof(disposable)));
                }
                disposable.Add(Observable.Concat(processes).Subscribe(observer));
                await RunProcess();

                processes.OnCompleted();

                return () =>
                {
                    disposable.Dispose();
                };
            }).Publish().RefCount();
        }

        public IObservable<IRepositoryActionEntry> Process()
        {
            return this.process;
        }


        protected StaticRepositoryActionEntry AppendMessage(string message, bool isError = false)
        {
            return AppendProcess(new StaticRepositoryActionEntry(message, isError));
        }

        protected RepositoryActionReactiveProcessEntry AppendProcess(IReactiveProcess process)
        {
            return AppendProcess(new RepositoryActionReactiveProcessEntry(process));
        }

        protected T AppendProcess<T>(T process)
            where T : IRepositoryActionEntry
        {
            this.processes.OnNext(process);
            return process;
        }

        protected abstract Task RunProcess();
    }
}
