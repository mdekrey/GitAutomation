import { BasicBranch } from "./basic-branch";

export interface BranchDetails extends BasicBranch {
  conflictResolutionMode: string;
  directDownstreamBranches: BasicBranch[];
  downstreamBranches: BasicBranch[];
  directUpstreamBranches: BasicBranch[];
  upstreamBranches: BasicBranch[];
}
