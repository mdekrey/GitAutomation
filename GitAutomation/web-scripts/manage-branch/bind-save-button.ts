import { difference, intersection } from "../utils/ramda";
import { Observable } from "../utils/rxjs";

import { IManageBranch } from "./data";
import { updateBranch } from "../api/basics";

export interface ISaveData {
  downstream: string[];
  upstream: string[];
  branchName: string;
  upstreamMergePolicy: GitAutomationGQL.IUpstreamMergePolicyEnum;
  branchType: GitAutomationGQL.IBranchGroupTypeEnum;
}

export const doSave = (
  data: Observable<ISaveData>,
  branchData: Observable<IManageBranch>
) =>
  data
    .take(1)
    // TODO - warn in this case, but we can't allow saving with
    // upstream and downstream having the same branch.
    .filter(
      ({ upstream, downstream }) => !intersection(upstream, downstream).length
    )
    .withLatestFrom(
      branchData.map(d => d.branches),
      (
        { branchName, upstream, downstream, upstreamMergePolicy, branchType },
        branches
      ) => {
        const oldUpstream = branches
          .filter(b => b.isUpstream)
          .map(b => b.groupName);
        const oldDownstream = branches
          .filter(b => b.isDownstream)
          .map(b => b.groupName);
        return {
          branchName,
          upstreamMergePolicy,
          branchType,
          addUpstream: difference(upstream, oldUpstream),
          removeUpstream: difference(oldUpstream, upstream),
          addDownstream: difference(downstream, oldDownstream),
          removeDownstream: difference(oldDownstream, downstream)
        };
      }
    )
    .switchMap(({ branchName, ...requestBody }) =>
      updateBranch(branchName, requestBody).map(_ => branchName)
    );
