using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace GitAutomation.BranchSettings
{
    public interface IBranchSettings
    {
        IObservable<string[]> GetConfiguredBranches();
        IObservable<string[]> GetDownstreamBranches(string branchName);
        IObservable<string[]> GetUpstreamBranches(string branchName);

        void AddBranchSetting(string upstreamBranch, string downstreamBranch, Work.IUnitOfWork work);
        void RemoveBranchSetting(string upstreamBranch, string downstreamBranch, Work.IUnitOfWork work);

    }
}
