using GitAutomation.Processes;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace GitAutomation.Orchestration.Actions
{
    internal abstract class ComplexAction
    {
        private readonly IObservable<OutputMessage> process;
        private readonly Subject<IObservable<OutputMessage>> processes;

        public ComplexAction()
        {
            var disposable = new CompositeDisposable();
            this.processes = new Subject<IObservable<OutputMessage>>();
            disposable.Add(processes);

            this.process = Observable.Create<OutputMessage>(async (observer, cancellationToken) =>
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

        public IObservable<OutputMessage> Process()
        {
            return this.process;
        }

        protected IObservable<OutputMessage> AppendProcess(IObservable<OutputMessage> process)
        {
            this.processes.OnNext(process);
            return process;
        }

        protected abstract Task RunProcess();
    }
}
