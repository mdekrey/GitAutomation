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
      const directDownstreamBranches =
        branchDetails.directDownstreamBranchGroups;
      const upstreamBranches = branchDetails.upstreamBranchGroups;
      const directUpstreamBranches = branchDetails.directUpstreamBranchGroups;
      const downstreamBranches = branchDetails.downstreamBranchGroups;
      return {
        branches: allBranches.map(({ groupName }): IBranchData => ({
          branch: groupName,
          isDownstream: directDownstreamBranches.indexOf(groupName) >= 0,
          isDownstreamAllowed: upstreamBranches.indexOf(groupName) == -1,
          isUpstream: directUpstreamBranches.indexOf(groupName) >= 0,
          isSomewhereUpstream: Boolean(
            branchDetails.upstreamBranchGroups.find(
              branch => branch === groupName
            )
          ),
          isUpstreamAllowed: downstreamBranches.indexOf(groupName) == -1
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
