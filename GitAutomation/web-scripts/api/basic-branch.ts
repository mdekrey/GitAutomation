export enum BranchType {
  ServiceLine = "ServiceLine",
  Hotfix = "Hotfix",
  Infrastructure = "Infrastructure",
  Feature = "Feature",
  Integration = "Integration",
  ReleaseCandidate = "ReleaseCandidate"
}

export enum StatusState {
  Success,
  Error,
  Pending
}
export interface CommitStatus {
  key: string;
  description: string;
  url: string;
  state: StatusState;
}
export interface CommitRef {
  name: string;
  commit: string;
}

export interface BranchGroupWithHierarchy {
  branchType: GitAutomationGQL.IBranchGroupTypeEnum;
  groupName: string;
  directDownstream: string[];
  downstream: string[];
  directUpstream: string[];
  upstream: string[];
  hierarchyDepth: number;
}
