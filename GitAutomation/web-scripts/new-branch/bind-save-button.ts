import { Observable } from "../utils/rxjs";

import { createBranch } from "../api/basics";

export interface ISaveData {
  downstream: string[];
  upstream: string[];
  branchName: string;
  recreateFromUpstream: boolean;
  branchType: GitAutomationGQL.IBranchGroupTypeEnum;
}

export const doSave = (data: Observable<ISaveData>) => {
  // save
  return data
    .take(1)
    .map(({ branchName, upstream, recreateFromUpstream, branchType }) => {
      return {
        branchName,
        requestBody: {
          recreateFromUpstream,
          branchType,
          addUpstream: upstream
        }
      };
    })
    .switchMap(({ branchName, requestBody }) =>
      createBranch(branchName, requestBody).map(_ => branchName)
    );
};
