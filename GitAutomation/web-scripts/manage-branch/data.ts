import { Observable, Subscription } from "rxjs";

import { allBranches, branchDetails } from "../api/basics";

export interface IManageBranch {
  isLoading: boolean;
  recreateFromUpstream: boolean;
  branchType: string;
  branches: IBranchData[];
}

export interface IBranchData {
  branch: string;
  isDownstream: boolean;
  isUpstream: boolean;
  isDownstreamAllowed: boolean;
  isUpstreamAllowed: boolean;
}

export const runBranchData = (branchName: string, reload: Observable<any>) => {
  const subscription = new Subscription();

  const initializeBranchData = allBranches()
    .combineLatest(branchDetails(branchName), (allBranches, branchDetails) => ({
      branches: allBranches.map(({ branchName }): IBranchData => ({
        branch: branchName,
        isDownstream:
          branchDetails.directDownstreamBranches.indexOf(branchName) >= 0,
        isDownstreamAllowed:
          branchDetails.upstreamBranches.indexOf(branchName) == -1,
        isUpstream:
          branchDetails.directUpstreamBranches.indexOf(branchName) >= 0,
        isUpstreamAllowed:
          branchDetails.downstreamBranches.indexOf(branchName) == -1
      })),
      branchType: branchDetails.branchType,
      recreateFromUpstream: branchDetails.recreateFromUpstream,
      conflictResolutionMode: branchDetails.conflictResolutionMode
    }))
    .map(
      ({
        branches,
        recreateFromUpstream,
        branchType,
        conflictResolutionMode
      }): IManageBranch => ({
        branches,
        recreateFromUpstream,
        branchType,
        isLoading: false
      })
    );

  const branchData = Observable.of<IManageBranch>({
    isLoading: true,
    recreateFromUpstream: false,
    branchType: "Feature",
    branches: []
  })
    .concat(reload.startWith(null).switchMap(() => initializeBranchData))
    .publishReplay(1)
    .refCount();

  return {
    state: branchData,
    subscription
  };
};
