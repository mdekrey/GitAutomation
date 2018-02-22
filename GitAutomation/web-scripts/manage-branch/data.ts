import { flatten } from "../utils/ramda";
import { Observable } from "../utils/rxjs";

import { allBranchGroups, branchDetails } from "../api/basics";
import { groupsToHierarchy } from "../api/hierarchy";
import { IBranchData } from "./branch-check-listing";
import { IBranchSettingsData } from "./branch-settings";

export interface IManageBranch {
  groupName: GitAutomationGQL.IBranchGroupDetails["groupName"];
  isLoading: boolean;
  upstreamMergePolicy: GitAutomationGQL.IBranchGroupDetails["upstreamMergePolicy"];
  branchType: GitAutomationGQL.IBranchGroupDetails["branchType"];
  otherBranches: IBranchData[];
  branches: Pick<GitAutomationGQL.IGitRef, "name" | "commit" | "url">[];
  latestBranch: { name: string } | null;
}

export function runBranchData(branchName: string, reload: Observable<any>) {
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
        const actualBranchNames = new Set<string>(
          branchDetails.branches.map(b => b.name)
        );
        const result = {
          otherBranches: allBranches.map((group): IBranchData => ({
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
            isUpstreamAllowed:
              downstreamBranches.indexOf(group.groupName) == -1,
            pullRequests: flatten(group.branches.map(b => b.pullRequestsInto))
              .filter(pr => actualBranchNames.has(pr.targetBranch))
              .filter(pr => pr.state === "Open")
              .sort(
                (a, b) =>
                  -(Number(a.id).toString() === a.id &&
                  Number(b.id).toString() === b.id
                    ? Number(a.id) - Number(b.id)
                    : a.id.localeCompare(b.id))
              )
          })),
          branchType: branchDetails.branchType,
          upstreamMergePolicy: branchDetails.upstreamMergePolicy,
          branches: branchDetails.branches,
          latestBranch: branchDetails.latestBranch
            ? branchDetails.latestBranch
            : null
        };
        return result;
      }
    )
    .map(
      ({
        otherBranches,
        upstreamMergePolicy,
        branchType,
        branches,
        latestBranch
      }): IManageBranch => ({
        groupName: branchName,
        otherBranches,
        upstreamMergePolicy,
        branchType,
        isLoading: false,
        branches,
        latestBranch
      })
    );

  const branchData = Observable.of<IManageBranch>({
    groupName: branchName,
    isLoading: true,
    upstreamMergePolicy: "None",
    branchType: "Feature",
    otherBranches: [],
    branches: [],
    latestBranch: null
  })
    .concat(reload.startWith(null).switchMap(() => initializeBranchData))
    .publishReplay(1)
    .refCount();

  return branchData;
}

export function fromBranchDataToGraph(
  branchData: Observable<IBranchData[]>,
  manageData: Observable<IBranchSettingsData>
) {
  const fullBranchData = Observable.combineLatest(manageData, branchData)
    .map(([{ groupName, branchType, upstreamMergePolicy }, branchData]) => ({
      branchName: groupName || "New Branch",
      upstreamMergePolicy,
      branchType,
      downstream: branchData
        .filter(branch => branch.isDownstream)
        .map(branch => branch.groupName),
      upstream: branchData
        .filter(branch => branch.isUpstream)
        .map(branch => branch.groupName)
    }))
    .publishReplay(1)
    .refCount();
  return fullBranchData
    .combineLatest(allBranchGroups, (newStatus, groups) => ({
      groups: groups
        .map(
          group =>
            group.groupName === newStatus.branchName
              ? {
                  ...group,
                  branchType: newStatus.branchType,
                  directDownstream: newStatus.downstream.map(groupName => ({
                    groupName
                  }))
                }
              : {
                  ...group,
                  directDownstream: group.directDownstream
                    .filter(g => g.groupName !== newStatus.branchName)
                    .concat(
                      newStatus.upstream.find(up => up === group.groupName)
                        ? [{ groupName: newStatus.branchName }]
                        : []
                    )
                }
        )
        .concat(
          groups.find(group => group.groupName === newStatus.branchName)
            ? []
            : [
                {
                  groupName: newStatus.branchName,
                  branchType: newStatus.branchType,
                  directDownstream: newStatus.downstream.map(groupName => ({
                    groupName
                  })),
                  latestBranch: null,
                  branches: []
                }
              ]
        ),
      branchName: newStatus.branchName,
      branchType: newStatus.branchType
    }))
    .switchMap(groupsData =>
      groupsToHierarchy(
        Observable.of(groupsData.groups),
        group =>
          group.groupName === groupsData.branchName ||
          Boolean(group.upstream.find(v => v === groupsData.branchName)) ||
          Boolean(group.downstream.find(v => v === groupsData.branchName))
      )
    );
}
