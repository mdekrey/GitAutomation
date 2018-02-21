import { difference, intersection } from "../utils/ramda";
import { Observable } from "../utils/rxjs";

import { updateBranch } from "../api/basics";
import { IBranchData } from "./branch-check-listing";
import { IBranchSettingsData } from "./branch-settings";

export interface ISaveData {
  downstream: string[];
  upstream: string[];
  branchName: string;
  upstreamMergePolicy: GitAutomationGQL.IUpstreamMergePolicyEnum;
  branchType: GitAutomationGQL.IBranchGroupTypeEnum;
}

function toUpstreamDownstream(branches: IBranchData[]) {
  const upstream = branches.filter(b => b.isUpstream).map(b => b.groupName);
  const downstream = branches.filter(b => b.isDownstream).map(b => b.groupName);
  return { upstream, downstream };
}

export const doSave = (
  originalData: Observable<IBranchData[]>,
  data: Observable<IBranchSettingsData>,
  otherBranches: Observable<IBranchData[]>
) =>
  otherBranches
    .take(1)
    // TODO - warn in this case, but we can't allow saving with
    // upstream and downstream having the same branch.
    .map(toUpstreamDownstream)
    .filter(
      ({ upstream, downstream }) => !intersection(upstream, downstream).length
    )
    .withLatestFrom(
      data,
      originalData.map(toUpstreamDownstream),
      (
        { upstream, downstream },
        { groupName, upstreamMergePolicy, branchType },
        { upstream: oldUpstream, downstream: oldDownstream }
      ) => {
        return {
          groupName,
          upstreamMergePolicy,
          branchType,
          addUpstream: difference(upstream, oldUpstream),
          removeUpstream: difference(oldUpstream, upstream),
          addDownstream: difference(downstream, oldDownstream),
          removeDownstream: difference(oldDownstream, downstream)
        };
      }
    )
    .take(1)
    .switchMap(({ groupName, ...requestBody }) =>
      updateBranch(groupName, requestBody).map(_ => groupName)
    );
