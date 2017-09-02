using System;
using System.Reactive;

namespace GitAutomation.BranchSettings
{
    public interface IBranchSettingsNotifiers
    {
        IObservable<Unit> GetAnyNotification();
        IObservable<Unit> GetDownstreamBranchesChangedNotifier(string upstreamBranch);
        IObservable<Unit> GetUpstreamBranchesChangedNotifier(string downstreamBranch);
        void NotifyDownstreamBranchesChanged(string upstreamBranch);
        void NotifyUpstreamBranchesChanged(string downstreamBranch);
    }
}