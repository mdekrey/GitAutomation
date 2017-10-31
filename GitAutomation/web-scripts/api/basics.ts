import { Observable } from "../utils/rxjs";
import { OutputMessage } from "./output-message";
import { BranchGroup, BranchGroupWithHierarchy } from "./basic-branch";
import { ClaimDetails } from "./claim-details";
import { IUpdateUserRequestBody } from "./update-user";
import { indexBy } from "../utils/ramda";

export const currentClaims = () =>
  Observable.ajax("/api/authentication/claims").map(
    response => response.response as ClaimDetails
  );

export const signOut = () =>
  Observable.ajax("/api/authentication/sign-out").map(
    response => response.response as void
  );

export const allUsers = () =>
  Observable.ajax("/api/authenticationManagement/all-users").map(
    response => response.response as Record<string, string[]>
  );

export const updateUser = (userName: string, body: IUpdateUserRequestBody) =>
  Observable.ajax
    .put("/api/authenticationManagement/user/" + userName, body, {
      "Content-Type": "application/json"
    })
    .map(response => response.response as Record<string, string[]>);

export const actionQueue = () =>
  Observable.ajax("/api/management/queue").map(
    response =>
      response.response as { actionType: string; parameters: string[] }[]
  );

export const allBranches = () =>
  Observable.ajax("/api/management/all-branches").map(
    response => response.response as BranchGroup[]
  );

export const allBranchGroups = () =>
  Observable.ajax("/api/management/all-branches/hierarchy").map(
    response => response.response as BranchGroup[]
  );

interface TreeEntry {
  descendants: Set<string>;
  depth: number;
  ancestors: Set<string>;
}
const newTreeEntry = (): TreeEntry => ({
  descendants: new Set<string>(),
  depth: 0,
  ancestors: new Set<string>()
});
const buildDescendantsTree = <T>(
  items: T[],
  getKey: (entry: T) => string,
  getChildrenKeys: (entry: T) => string[]
) => {
  const lookup = indexBy(getKey, items);
  const result: Record<string, TreeEntry> = {};
  const getOrCreate = (key: string) =>
    (result[key] = result[key] || newTreeEntry());
  const enqueued = new Set<string>(items.map(getKey));
  const addTo = (target: Set<string>) => (entry: string) => target.add(entry);
  while (enqueued.size > 0) {
    const next = Array.from(enqueued)[0];
    enqueued.delete(next);
    const target = getOrCreate(next);
    getChildrenKeys(lookup[next]).forEach(childKey => {
      target.descendants.add(childKey);
      const child = getOrCreate(childKey);
      child.descendants.forEach(addTo(target.descendants));
      if (!child.ancestors.has(next)) {
        enqueued.add(childKey);
      }
      child.ancestors.add(next);
      target.ancestors.forEach(addTo(child.ancestors));
    });
    const originalDepth = target.depth;
    target.ancestors.forEach(ancestor => {
      enqueued.add(ancestor);
      target.depth = Math.max(target.depth, getOrCreate(ancestor).depth + 1);
    });
    if (target.depth != originalDepth) {
      enqueued.add(next);
      target.descendants.forEach(addTo(enqueued));
    }
  }
  return result;
};

export const allBranchesHierarchy: () => Observable<
  Record<string, BranchGroupWithHierarchy>
> = () =>
  allBranchGroups().map(branches => {
    const directUpstreamTree = indexBy(
      b => b.groupName,
      branches.map(b => ({
        groupName: b.groupName,
        upstream: branches
          .filter(other =>
            (other.directDownstreamBranchGroups || []).find(
              d => d === b.groupName
            )
          )
          .map(other => other.groupName)
      }))
    );

    const downstreamTree = buildDescendantsTree(
      branches,
      b => b.groupName,
      b => (b ? b.directDownstreamBranchGroups : null) || []
    );
    console.log(downstreamTree);

    return indexBy(
      g => g.groupName,
      branches.map((branch): BranchGroupWithHierarchy => ({
        branchType: branch.branchType,
        groupName: branch.groupName,
        directDownstream: branch.directDownstreamBranchGroups,
        downstream: Array.from(downstreamTree[branch.groupName].descendants),
        upstream: Array.from(downstreamTree[branch.groupName].ancestors),
        directUpstream: directUpstreamTree[branch.groupName].upstream,
        hierarchyDepth: downstreamTree[branch.groupName].depth
      }))
    );
  });

export const branchDetails = (branchName: string) =>
  Observable.ajax("/api/management/details/" + branchName).map(
    response => response.response as BranchGroup
  );

export const detectUpstream = (branchName: string, asGroup: boolean) =>
  Observable.ajax(
    "/api/management/detect-upstream/" + branchName + `?asGroup=${asGroup}`
  ).map(response => response.response as string[]);

export const detectAllUpstream = (branchName: string) =>
  Observable.ajax("/api/management/detect-all-upstream/" + branchName).map(
    response => response.response as string[]
  );

export const checkPullRequests = (branchName: string) =>
  Observable.ajax("/api/management/check-prs/" + branchName).map(
    response =>
      response.response as {
        reviews: { username: string; state: string[] }[];
        state: string;
        sourceBranch: string;
        targetBranch: string;
        id: string;
      }[]
  );

export const getLog = () =>
  Observable.ajax("/api/management/log").map(
    response => response.response as OutputMessage[]
  );

export const fetch = () =>
  Observable.ajax
    .post("/api/gitwebhook")
    .map(response => response.response as null);

export const createBranch = (
  branchName: string,
  body: {
    recreateFromUpstream: boolean;
    branchType: string;
    addUpstream: string[];
  }
) =>
  Observable.ajax
    .put("/api/management/branch/create/" + branchName, body, {
      "Content-Type": "application/json"
    })
    .map(response => response.response as null);

export const updateBranch = (
  branchName: string,
  body: {
    recreateFromUpstream: boolean;
    branchType: string;
    addUpstream: string[];
    addDownstream: string[];
    removeUpstream: string[];
    removeDownstream: string[];
  }
) =>
  Observable.ajax
    .put("/api/management/branch/propagation/" + branchName, body, {
      "Content-Type": "application/json"
    })
    .map(response => response.response as null);

export const checkDownstreamMerges = (branchName: string) =>
  Observable.ajax
    .put("/api/management/branch/check-upstream/" + branchName)
    .map(response => response.response as null);

export const promoteServiceLine = (body: {
  releaseCandidate: string;
  serviceLine: string;
  tagName: string;
  autoConsolidate: boolean;
}) =>
  Observable.ajax
    .put("/api/management/branch/promote", body, {
      "Content-Type": "application/json"
    })
    .map(response => response.response as null);

export const consolidateMerged = ({
  targetBranch,
  originalBranches
}: {
  targetBranch: string;
  originalBranches: string[];
}) =>
  Observable.ajax
    .put(
      "/api/management/branch/consolidate/" + targetBranch,
      originalBranches,
      {
        "Content-Type": "application/json"
      }
    )
    .map(response => response.response as null);

export const deleteBranch = (branchName: string) =>
  Observable.ajax
    .delete("/api/management/branch/" + branchName)
    .map(response => response.response as null);

export const recommendGroups = () =>
  Observable.ajax
    .get("/api/management/recommend-groups")
    .map(response => response.response as string[]);
