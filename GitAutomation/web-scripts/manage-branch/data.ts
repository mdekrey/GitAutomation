import { Observable, Subscription } from "rxjs";

import { allBranches, branchDetails } from "../api/basics";
import { BranchGroup } from "../api/basic-branch";

export interface IManageBranch {
  isLoading: boolean;
  recreateFromUpstream: boolean;
  branchType: string;
  branches: IBranchData[];
  branchNames: string[];
  latestBranchName: string | null;
}

export interface IBranchData extends BranchGroup {
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
        branches: allBranches.map((group): IBranchData => ({
          ...group,
          isDownstream: directDownstreamBranches.indexOf(group.groupName) >= 0,
          isDownstreamAllowed: upstreamBranches.indexOf(group.groupName) == -1,
          isUpstream: directUpstreamBranches.indexOf(group.groupName) >= 0,
          isSomewhereUpstream: Boolean(
            branchDetails.upstreamBranchGroups.find(
              branch => branch === group.groupName
            )
          ),
          isUpstreamAllowed: downstreamBranches.indexOf(group.groupName) == -1
        })),
        branchType: branchDetails.branchType,
        recreateFromUpstream: branchDetails.recreateFromUpstream,
        branchNames: branchDetails.branchNames,
        latestBranchName: branchDetails.latestBranchName
      };
    })
    .map(
      ({
        branches,
        recreateFromUpstream,
        branchType,
        branchNames,
        latestBranchName
      }): IManageBranch => ({
        branches,
        recreateFromUpstream,
        branchType,
        isLoading: false,
        branchNames,
        latestBranchName
      })
    );

  const branchData = Observable.of<IManageBranch>({
    isLoading: true,
    recreateFromUpstream: false,
    branchType: "Feature",
    branches: [],
    branchNames: [],
    latestBranchName: null
  })
    .concat(reload.startWith(null).switchMap(() => initializeBranchData))
    .publishReplay(1)
    .refCount();

  return {
    state: branchData,
    subscription
  };
};
