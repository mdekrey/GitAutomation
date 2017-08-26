import { BasicBranch } from "./basic-branch";

export interface BranchDetails extends BasicBranch {
  conflictResolutionMode: string;
  directDownstreamBranches: string[];
  downstreamBranches: string[];
  directUpstreamBranches: string[];
  upstreamBranches: string[];
}
