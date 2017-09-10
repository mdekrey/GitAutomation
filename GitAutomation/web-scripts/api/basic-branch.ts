export enum BranchType {
  ServiceLine = "ServiceLine",
  Hotfix = "Hotfix",
  Infrastructure = "Infrastructure",
  Feature = "Feature",
  Integration = "Integration",
  ReleaseCandidate = "ReleaseCandidate"
}

export interface BasicBranch {
  recreateFromUpstream: boolean;
  branchType: BranchType;
  branchName: string;
  branchNames: string[];
}
