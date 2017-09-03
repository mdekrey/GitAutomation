using GitAutomation.Processes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace GitAutomation.Orchestration
{
    class RepositoryOrchestration : IRepositoryOrchestration
    {
        enum QueueAlterationKind
        {
            Remove,
            Append,
        }
        struct QueueAlteration
        {
            public QueueAlterationKind Kind;
            public IRepositoryAction Target;
        }

        const int logLength = 300;

        private readonly IObservable<ImmutableList<IRepositoryAction>> repositoryActions;
        private readonly IObservable<OutputMessage> repositoryActionProcessor;
        private readonly IObservable<ImmutableList<OutputMessage>> repositoryActionProcessorLog;
        private readonly IObserver<QueueAlteration> queueAlterations;

        public RepositoryOrchestration(IServiceProvider serviceProvider)
        {
            var queueAlterations = new Subject<QueueAlteration>();
            this.queueAlterations = queueAlterations;

            var repositoryActions = queueAlterations.Scan(ImmutableList<IRepositoryAction>.Empty, (list, alteration) =>
            {
                if (alteration.Kind == QueueAlterationKind.Append)
                {
                    return list.Add(alteration.Target);
                }
                else if (alteration.Kind == QueueAlterationKind.Remove)
                {
                    return list.Remove(alteration.Target);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }).Replay(1);
            repositoryActions.Connect();
            this.repositoryActions = repositoryActions;

            this.repositoryActionProcessor = repositoryActions.Select(action => action.FirstOrDefault()).DistinctUntilChanged()
                .Select(action => action == null
                    ? Observable.Empty<OutputMessage>()
                    : action.PerformAction(serviceProvider).Catch<OutputMessage, Exception>(ex =>
                    {
                        // TODO - better logging
                        return Observable.Return(new OutputMessage { Channel = OutputChannel.Error, Message = $"Action {action.ActionType} encountered an exception: {ex.Message}" });
                    }).Finally(() =>
                    {
                        this.queueAlterations.OnNext(new QueueAlteration { Kind = QueueAlterationKind.Remove, Target = action });
                    }))
                .Switch().Publish().RefCount();

            var temp = repositoryActionProcessor
                .Scan(
                    ImmutableList<OutputMessage>.Empty,
                    (list, next) =>
                        (
                            list.Count >= logLength
                                ? list.RemoveRange(0, list.Count - (logLength - 1))
                                : list
                        ).Add(next)
                ).Replay(1);
            temp.Connect();
            this.repositoryActionProcessorLog = temp;
        }

        public IObservable<OutputMessage> ProcessActions()
        {
            return this.repositoryActionProcessor;
        }
        public IObservable<ImmutableList<OutputMessage>> ProcessActionsLog => this.repositoryActionProcessorLog;
        public IObservable<ImmutableList<IRepositoryAction>> ActionQueue => this.repositoryActions;

        public IObservable<OutputMessage> EnqueueAction(IRepositoryAction resetAction)
        {
            this.queueAlterations.OnNext(new QueueAlteration { Kind = QueueAlterationKind.Append, Target = resetAction });
            return resetAction.DeferredOutput;
        }

    }
}
