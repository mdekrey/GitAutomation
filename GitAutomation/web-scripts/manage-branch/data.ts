import { Observable, Subscription } from "rxjs";
import { flatten, uniq } from "ramda";

import {
  remoteBranches,
  downstreamBranches,
  upstreamBranches
} from "../api/basics";

export interface IManageBranch {
  isLoading: boolean;
  branches: IBranchData[];
}

export interface IBranchData {
  branch: string;
  isDownstream: boolean;
  isUpstream: boolean;
}

export const runBranchData = (branchName: string) => {
  const subscription = new Subscription();

  const initializeBranchData = remoteBranches()
    .combineLatest(
      downstreamBranches(branchName),
      upstreamBranches(branchName),
      (allBranches, downstreamBranches, upstreamBranches) =>
        uniq(
          flatten<string>([allBranches, downstreamBranches, upstreamBranches])
        ).map((branch): IBranchData => ({
          branch,
          isDownstream: downstreamBranches.indexOf(branch) >= 0,
          isUpstream: upstreamBranches.indexOf(branch) >= 0
        }))
    )
    .map((branches): IManageBranch => ({
      branches,
      isLoading: false
    }));

  const branchData = Observable.of<IManageBranch>({
    isLoading: true,
    branches: []
  })
    .concat(initializeBranchData)
    .publishReplay(1)
    .refCount();

  return {
    state: branchData,
    subscription
  };
};
