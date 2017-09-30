import { BasicBranch } from "./basic-branch";

export interface BranchHierarchy extends BasicBranch {
  hierarchyDepth: number;
  downstreamBranchGroups: string[];
}
