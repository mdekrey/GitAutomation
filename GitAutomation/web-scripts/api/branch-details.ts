import { BasicBranch } from "./basic-branch";

export interface BranchDetails extends BasicBranch {
  conflictResolutionMode: string;
  directDownstreamBranchGroups: string[];
  downstreamBranchGroups: string[];
  directUpstreamBranchGroups: string[];
  upstreamBranchGroups: string[];
}
