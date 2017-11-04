export interface BranchGroupWithDownstream {
  groupName: string;
  branchType: GitAutomationGQL.IBranchGroupTypeEnum;
  latestBranch: {
    name: string;
  } | null;
  branches: GitAutomationGQL.IGitRef[];
  directDownstream: Pick<GitAutomationGQL.IBranchGroupDetails, "groupName">[];
}
