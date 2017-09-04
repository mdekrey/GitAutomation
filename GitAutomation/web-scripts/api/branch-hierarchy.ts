import { BasicBranch } from "./basic-branch";

export interface BranchHierarchy extends BasicBranch {
  downstreamBranches: string[];
}
