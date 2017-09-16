import { Observable, Subscription } from "rxjs";

import { allBranches, branchDetails } from "../api/basics";

export interface IManageBranch {
  isLoading: boolean;
  recreateFromUpstream: boolean;
  branchType: string;
  branches: IBranchData[];
  branchNames: string[];
}

export interface IBranchData {
  branch: string;
  isDownstream: boolean;
  isUpstream: boolean;
  isSomewhereUpstream: boolean;
  isDownstreamAllowed: boolean;
  isUpstreamAllowed: boolean;
}

export const runBranchData = (branchName: string, reload: Observable<any>) => {
  const subscription = new Subscription();

  const initializeBranchData = allBranches()
    .combineLatest(branchDetails(branchName), (allBranches, branchDetails) => {
      const directDownstreamBranches = branchDetails.directDownstreamBranches.map(
        b => b.branchName
      );
      const upstreamBranches = branchDetails.upstreamBranches.map(
        b => b.branchName
      );
      const directUpstreamBranches = branchDetails.directUpstreamBranches.map(
        b => b.branchName
      );
      const downstreamBranches = branchDetails.downstreamBranches.map(
        b => b.branchName
      );
      return {
        branches: allBranches.map(({ branchName }): IBranchData => ({
          branch: branchName,
          isDownstream: directDownstreamBranches.indexOf(branchName) >= 0,
          isDownstreamAllowed: upstreamBranches.indexOf(branchName) == -1,
          isUpstream: directUpstreamBranches.indexOf(branchName) >= 0,
          isSomewhereUpstream: Boolean(
            branchDetails.upstreamBranches.find(
              branch => branch.branchName === branchName
            )
          ),
          isUpstreamAllowed: downstreamBranches.indexOf(branchName) == -1
        })),
        branchType: branchDetails.branchType,
        recreateFromUpstream: branchDetails.recreateFromUpstream,
        conflictResolutionMode: branchDetails.conflictResolutionMode,
        branchNames: branchDetails.branchNames
      };
    })
    .map(
      ({
        branches,
        recreateFromUpstream,
        branchType,
        conflictResolutionMode,
        branchNames
      }): IManageBranch => ({
        branches,
        recreateFromUpstream,
        branchType,
        isLoading: false,
        branchNames
      })
    );

  const branchData = Observable.of<IManageBranch>({
    isLoading: true,
    recreateFromUpstream: false,
    branchType: "Feature",
    branches: [],
    branchNames: []
  })
    .concat(reload.startWith(null).switchMap(() => initializeBranchData))
    .publishReplay(1)
    .refCount();

  return {
    state: branchData,
    subscription
  };
};
