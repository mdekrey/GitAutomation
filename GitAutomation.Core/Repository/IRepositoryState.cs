﻿using GitAutomation.Processes;
using System;
using System.Collections.Immutable;

namespace GitAutomation.Repository
{
    public interface IRepositoryState
    {
        IObservable<OutputMessage> ProcessActions();
        IObservable<ImmutableList<OutputMessage>> ProcessActionsLog { get; }
        IObservable<ImmutableList<IRepositoryAction>> ActionQueue { get; }

        IObservable<OutputMessage> DeleteBranch(string branchName);
        IObservable<OutputMessage> DeleteRepository();
        IObservable<OutputMessage> CheckForUpdates();

        IObservable<string[]> RemoteBranches();
        IObservable<OutputMessage> CheckDownstreamMerges(string downstreamBranch);
        IObservable<OutputMessage> CheckAllDownstreamMerges();

        IObservable<OutputMessage> ConsolidateServiceLine(string releaseCandidateBranch, string serviceLineBranch, string tagName);

    }
}