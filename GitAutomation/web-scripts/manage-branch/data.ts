import { Observable, Subscription } from "../utils/rxjs";

import { allBranchGroups, branchDetails } from "../api/basics";

export interface IManageBranch {
  isLoading: boolean;
  recreateFromUpstream: boolean;
  branchType: string;
  branches: IBranchData[];
  actualBranches: Pick<GitAutomationGQL.IGitRef, "name" | "commit">[];
  latestBranchName: string | null;
}

export interface IBranchData {
  groupName: string;
  branchType: GitAutomationGQL.IBranchGroupTypeEnum;
  latestBranch: {
    name: string;
  } | null;
  branches: GitAutomationGQL.IGitRef[];
  isDownstream: boolean;
  isUpstream: boolean;
  isSomewhereUpstream: boolean;
  isDownstreamAllowed: boolean;
  isUpstreamAllowed: boolean;
}

export const runBranchData = (branchName: string, reload: Observable<any>) => {
  const subscription = new Subscription();

  const initializeBranchData = allBranchGroups()
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
        actualBranches: branchDetails.branches,
        latestBranchName: branchDetails.latestBranchName
      };
    })
    .map(
      ({
        branches,
        recreateFromUpstream,
        branchType,
        actualBranches,
        latestBranchName
      }): IManageBranch => ({
        branches,
        recreateFromUpstream,
        branchType,
        isLoading: false,
        actualBranches,
        latestBranchName
      })
    );

  const branchData = Observable.of<IManageBranch>({
    isLoading: true,
    recreateFromUpstream: false,
    branchType: "Feature",
    branches: [],
    actualBranches: [],
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
