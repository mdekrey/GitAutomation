import { ascend, sortWith } from "../utils/ramda";
import { BranchType } from "../api/basic-branch";

type BranchDetails = Pick<
  GitAutomationGQL.IBranchGroupDetails,
  "branchType" | "groupName"
>;

const branchTypes = [
  BranchType.ServiceLine,
  BranchType.Infrastructure,
  BranchType.Hotfix,
  BranchType.Feature,
  BranchType.Integration,
  BranchType.ReleaseCandidate
];

export const sortBranches = sortWith<BranchDetails>([
  ascend<BranchDetails>(b => branchTypes.indexOf(b.branchType as BranchType)),
  ascend<BranchDetails>(b => b.groupName)
]) as <T extends BranchDetails>(v: T[]) => T[];
