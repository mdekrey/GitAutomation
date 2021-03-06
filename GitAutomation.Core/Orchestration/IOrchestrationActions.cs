﻿using System;
using GitAutomation.Processes;
using System.Collections.Generic;
using GitAutomation.Repository;

namespace GitAutomation.Orchestration
{
    public interface IOrchestrationActions
    {
        IObservable<IRepositoryActionEntry> CheckForUpdates();
        IObservable<IRepositoryActionEntry> DeleteBranch(string branchName, DeleteBranchMode mode);
        IObservable<IRepositoryActionEntry> DeleteRepository();
        IObservable<IRepositoryActionEntry> CheckDownstreamMerges(string downstreamBranch);
        IObservable<IRepositoryActionEntry> ReleaseToServiceLine(string releaseCandidateBranch, string serviceLineBranch, string tagName, bool autoConsolidate);
        IObservable<IRepositoryActionEntry> ConsolidateMerged(string sourceBranch, string newBaseBranch);
        void CheckForUpdatesOnBranch(string branchName);
    }
}