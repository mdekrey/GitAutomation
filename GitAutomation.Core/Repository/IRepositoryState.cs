using GitAutomation.Processes;
using System;
using System.Collections.Immutable;

namespace GitAutomation.Repository
{
    public interface IRepositoryState
    {
        IObservable<OutputMessage> DeleteBranch(string branchName);
        IObservable<OutputMessage> DeleteRepository();
        IObservable<OutputMessage> CheckForUpdates();

        IObservable<string[]> RemoteBranches();

    }
}