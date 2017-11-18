import { Observable, Subscription } from "../utils/rxjs";

import { allBranchGroups, branchDetails } from "../api/basics";
import { groupsToHierarchy } from "../api/hierarchy";

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

  const initializeBranchData = allBranchGroups
    .combineLatest(
      branchDetails(branchName),
      allBranchGroups.let(groupsToHierarchy),
      (allBranches, branchDetails, hierarchyData) => {
        const directDownstreamBranches = branchDetails.directDownstream.map(
          g => g.groupName
        );
        const target = hierarchyData[branchDetails.groupName] || {
          upstream: [],
          downstream: []
        };
        const upstreamBranches = target.upstream;
        const directUpstreamBranches = branchDetails.directUpstream.map(
          g => g.groupName
        );
        const downstreamBranches = target.downstream;
        const result = {
          branches: allBranches.map((group): IBranchData => ({
            groupName: group.groupName,
            branchType: group.branchType,
            latestBranch: group.latestBranch,
            branches: group.branches,
            isDownstream:
              directDownstreamBranches.indexOf(group.groupName) >= 0,
            isDownstreamAllowed:
              upstreamBranches.indexOf(group.groupName) == -1,
            isUpstream: directUpstreamBranches.indexOf(group.groupName) >= 0,
            isSomewhereUpstream: Boolean(
              target.upstream.find(branch => branch === group.groupName)
            ),
            isUpstreamAllowed: downstreamBranches.indexOf(group.groupName) == -1
          })),
          branchType: branchDetails.branchType,
          recreateFromUpstream: branchDetails.recreateFromUpstream,
          actualBranches: branchDetails.branches,
          latestBranchName: branchDetails.latestBranch
            ? branchDetails.latestBranch.name
            : null
        };
        console.log({ allBranches, branchDetails, hierarchyData, result });
        return result;
      }
    )
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
