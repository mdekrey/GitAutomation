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
            AppendNoDuplicateCheck,
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
                switch (alteration.Kind)
                {
                    case QueueAlterationKind.Append:
                        if (alteration.Target is IUniqueAction uniqueAction)
                        {
                            var existingTarget = list.FirstOrDefault(entry => entry.ActionType == alteration.Target.ActionType && entry.Parameters.ToString() == alteration.Target.Parameters.ToString());
                            if (existingTarget != null)
                            {
                                // TODO - should probably notify that the output is deferred. 
                                uniqueAction.AbortAs(existingTarget.DeferredOutput);
                                return list;
                            }
                        }
                        return list.Add(alteration.Target);
                    case QueueAlterationKind.AppendNoDuplicateCheck:
                        return list.Add(alteration.Target);
                    case QueueAlterationKind.Remove:
                        return list.Remove(alteration.Target);
                    default:
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

        public IObservable<OutputMessage> EnqueueAction(IRepositoryAction resetAction, bool skipDuplicateCheck)
        {
            this.queueAlterations.OnNext(new QueueAlteration
            {
                Kind = skipDuplicateCheck ? QueueAlterationKind.AppendNoDuplicateCheck : QueueAlterationKind.Append,
                Target = resetAction
            });
            return resetAction.DeferredOutput;
        }
    }
}
