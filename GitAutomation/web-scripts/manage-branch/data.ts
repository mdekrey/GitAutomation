import { Observable, Subscription } from "rxjs";

import { allBranches, branchDetails } from "../api/basics";

export interface IManageBranch {
  isLoading: boolean;
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
    .combineLatest(branchDetails(branchName), (allBranches, branchDetails) =>
      allBranches.map((branch): IBranchData => ({
        branch,
        isDownstream:
          branchDetails.directDownstreamBranches.indexOf(branch) >= 0,
        isDownstreamAllowed:
          branchDetails.upstreamBranches.indexOf(branch) == -1,
        isUpstream: branchDetails.directUpstreamBranches.indexOf(branch) >= 0,
        isUpstreamAllowed:
          branchDetails.downstreamBranches.indexOf(branch) == -1
      }))
    )
    .do(_ => console.log(_))
    .map((branches): IManageBranch => ({
      branches,
      isLoading: false
    }));

  const branchData = Observable.of<IManageBranch>({
    isLoading: true,
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
