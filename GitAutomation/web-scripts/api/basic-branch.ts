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

export interface BranchGroup {
  recreateFromUpstream: boolean;
  branchType: BranchType;
  groupName: string;
  branchNames: string[];
  latestBranchName: string | null;
  directDownstreamBranchGroups: string[];
  downstreamBranchGroups: string[];
  directUpstreamBranchGroups: string[];
  upstreamBranchGroups: string[];
  hierarchyDepth: number;
  statuses: CommitStatus[];
}
