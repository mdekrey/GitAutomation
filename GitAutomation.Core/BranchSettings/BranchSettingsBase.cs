using System;
using GitAutomation.Work;
using System.Reactive;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Collections.Concurrent;
using System.Reactive.Linq;

namespace GitAutomation.BranchSettings
{
    class BranchSettingsNotifiers : IBranchSettingsNotifiers
    {
        private readonly Subject<Unit> anyBranchesChangedNotifiers = new Subject<Unit>();
        private readonly ConcurrentDictionary<string, Subject<Unit>> downstreamBranchesChangedNotifiers = new ConcurrentDictionary<string, Subject<Unit>>();
        private readonly ConcurrentDictionary<string, Subject<Unit>> upstreamBranchesChangedNotifiers = new ConcurrentDictionary<string, Subject<Unit>>();

        public IObservable<Unit> GetAnyNotification()
        {
            return anyBranchesChangedNotifiers.AsObservable();
        }

        public IObservable<Unit> GetDownstreamBranchesChangedNotifier(string upstreamBranch)
        {
            return downstreamBranchesChangedNotifiers.GetOrAdd(upstreamBranch, _ => new Subject<Unit>()).AsObservable();
        }

        public IObservable<Unit> GetUpstreamBranchesChangedNotifier(string downstreamBranch)
        {
            return upstreamBranchesChangedNotifiers.GetOrAdd(downstreamBranch, _ => new Subject<Unit>()).AsObservable();
        }

        public void NotifyDownstreamBranchesChanged(string upstreamBranch)
        {
            if (downstreamBranchesChangedNotifiers.TryGetValue(upstreamBranch, out var target))
            {
                target.OnNext(Unit.Default);
            }
            anyBranchesChangedNotifiers.OnNext(Unit.Default);
        }

        public void NotifyUpstreamBranchesChanged(string downstreamBranch)
        {
            if (upstreamBranchesChangedNotifiers.TryGetValue(downstreamBranch, out var target))
            {
                target.OnNext(Unit.Default);
            }
            anyBranchesChangedNotifiers.OnNext(Unit.Default);
        }
    }
}