import gql from "graphql-tag";
import { Observable } from "../utils/rxjs";
import { BranchGroupWithHierarchy } from "./basic-branch";
import { ClaimDetails } from "./claim-details";
import { IUpdateUserRequestBody } from "./update-user";
import { graphQl, invalidateQuery } from "./graphql";
import { flatten, indexBy } from "../utils/ramda";

export const currentClaims = () =>
  graphQl<ClaimDetails>({
    query: gql`
      {
        claims: currentClaims {
          type
          value
        }
        roles: currentRoles
      }
    `,
    pollInterval: 300000
  }).filter(v => Boolean(v && v.claims && v.roles));

export const signOut = () =>
  Observable.ajax("/api/authentication/sign-out").map(
    response => response.response as void
  );

export const allUsers = () =>
  graphQl<Pick<GitAutomationGQL.IQuery, "users">>({
    query: gql`
      {
        users {
          username
          roles {
            role
          }
        }
      }
    `
  })
    .filter(v => Boolean(v && v.users))
    .map(r =>
      r.users.map(user => ({
        username: user.username,
        roles: user.roles.map(({ role }) => role)
      }))
    );

export const allRoles = () =>
  graphQl<{ roles: { role: string }[] }>({
    query: gql`
      {
        roles {
          role
        }
      }
    `,
    pollInterval: 0
  })
    .filter(v => Boolean(v && v.roles))
    .map(r => r.roles);

export const updateUser = (userName: string, body: IUpdateUserRequestBody) =>
  Observable.ajax
    .put("/api/authenticationManagement/user/" + userName, body, {
      "Content-Type": "application/json"
    })
    .do(() =>
      invalidateQuery(gql`
        {
          users {
            username
            roles {
              role
            }
          }
        }
      `)
    )
    .map(response => response.response as Record<string, string[]>);

export const actionQueue = () =>
  graphQl<Pick<GitAutomationGQL.IQuery, "orchestrationQueue">>({
    query: gql`
      {
        orchestrationQueue {
          actionType
          parameters
        }
      }
    `
  })
    .filter(v => Boolean(v && v.orchestrationQueue))
    .map(v => v.orchestrationQueue);

type AllBranchesQuery = {
  allActualBranches: GitAutomationGQL.IGitRef[];
  configuredBranchGroups: (Pick<
    GitAutomationGQL.IBranchGroupDetails,
    "groupName" | "branchType" | "latestBranch" | "branches"
  > & {
    directDownstream: Pick<GitAutomationGQL.IBranchGroupDetails, "groupName">[];
  })[];
};

export const allBranchGroups = () =>
  graphQl<AllBranchesQuery>({
    query: gql`
      {
        allActualBranches {
          name
          commit
        }
        configuredBranchGroups {
          groupName
          branchType
          directDownstream {
            groupName
          }
          latestBranch {
            name
          }
          branches {
            name
            commit
          }
        }
      }
    `
  })
    .filter(v => Boolean(v && v.allActualBranches && v.configuredBranchGroups))
    .map(result => {
      const configuredBranches = flatten<string>(
        result.configuredBranchGroups.map(g => g.branches.map(b => b.name))
      );
      const nonConfiguredBranches = result.allActualBranches.filter(
        b => configuredBranches.indexOf(b.name) === -1
      );
      return [
        ...result.configuredBranchGroups,
        ...nonConfiguredBranches.map(branch => ({
          groupName: branch.name,
          branchType: "Feature" as GitAutomationGQL.IBranchGroupTypeEnum,
          latestBranch: { name: branch.name },
          branches: [branch],
          directDownstream: [] as Pick<
            GitAutomationGQL.IBranchGroupDetails,
            "groupName"
          >[]
        }))
      ];
    });

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
            other.directDownstream.find(d => d.groupName === b.groupName)
          )
          .map(other => other.groupName)
      }))
    );

    const downstreamTree = buildDescendantsTree(
      branches,
      b => b.groupName,
      b => (b ? b.directDownstream.map(v => v.groupName) : [])
    );

    return indexBy(
      g => g.groupName,
      branches.map((branch): BranchGroupWithHierarchy => ({
        branchType: branch.branchType,
        groupName: branch.groupName,
        directDownstream: branch.directDownstream.map(
          downstream => downstream.groupName
        ),
        downstream: Array.from(downstreamTree[branch.groupName].descendants),
        upstream: Array.from(downstreamTree[branch.groupName].ancestors),
        directUpstream: directUpstreamTree[branch.groupName].upstream,
        hierarchyDepth: downstreamTree[branch.groupName].depth
      }))
    );
  });

export const branchDetails = (branchName: string) =>
  graphQl<Pick<GitAutomationGQL.IQuery, "branchGroup">>({
    query: gql`
      query($branchName: String!) {
        branchGroup(name: $branchName) {
          groupName
          recreateFromUpstream
          branchType
          directDownstream {
            groupName
          }
          directUpstream {
            groupName
          }
          latestBranch {
            name
          }
          branches {
            name
            commit
          }
        }
      }
    `,
    variables: {
      branchName
    }
  })
    .filter(v => Boolean(v && v.branchGroup))
    .map(g => g.branchGroup);

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
  graphQl<Pick<GitAutomationGQL.IQuery, "log">>({
    query: gql`
      {
        log {
          message
          exitCode
          channel
        }
      }
    `
  })
    .filter(v => Boolean(v && v.log))
    .map(g => g.log);

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

export const deleteBranchByMode = (
  branchName: string,
  mode: "ActualBranchOnly" | "GroupOnly"
) =>
  Observable.ajax
    .delete("/api/management/branch/" + branchName + "?mode=" + mode)
    .map(response => response.response as null);

export const recommendGroups = () =>
  Observable.ajax
    .get("/api/management/recommend-groups")
    .map(response => response.response as string[]);
