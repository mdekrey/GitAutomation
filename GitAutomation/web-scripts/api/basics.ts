import gql from "graphql-tag";
import { BehaviorSubject, Observable } from "../utils/rxjs";
import { ClaimDetails } from "./claim-details";
import { IUpdateUserRequestBody } from "./update-user";
import { graphQl, invalidateQuery } from "./graphql";
import { flatten, merge } from "../utils/ramda";
import { groupsToHierarchy } from "./hierarchy";
import { BranchGroupWithDownstream } from "./types";

export const application = graphQl<Pick<GitAutomationGQL.IQuery, "app">>(
  {
    query: gql`
      {
        app {
          title
        }
      }
    `,
    pollInterval: 0
  },
  { excludeErrors: true }
)
  .filter(v => Boolean(v))
  .map(v => v.app)
  .publishReplay(1);
application.connect();

export const currentClaims = graphQl<ClaimDetails>(
  {
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
  },
  { excludeErrors: true }
).filter(v => Boolean(v && v.claims && v.roles));

export const signOut = () =>
  Observable.ajax("/api/authentication/sign-out").map(
    response => response.response as void
  );

export const forceRefreshUsers = new BehaviorSubject<null>(null);
export const allUsers = forceRefreshUsers
  .switchMap(() =>
    graphQl<Pick<GitAutomationGQL.IQuery, "users">>(
      {
        query: gql`
          {
            users {
              username
              roles {
                role
              }
            }
          }
        `,
        pollInterval: 0
      },
      { excludeErrors: true }
    )
  )
  .filter(v => Boolean(v && v.users))
  .map(r =>
    r.users.map(user => ({
      username: user.username,
      roles: user.roles.map(({ role }) => role)
    }))
  );

export const forceRefreshRoles = new BehaviorSubject<null>(null);
export const allRoles = forceRefreshRoles
  .switchMap(() =>
    graphQl<{ roles: { role: string }[] }>(
      {
        query: gql`
          {
            roles {
              role
            }
          }
        `,
        pollInterval: 0
      },
      { excludeErrors: true }
    )
  )
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

export const actionQueue = graphQl<
  Pick<GitAutomationGQL.IQuery, "orchestrationQueue">
>(
  {
    query: gql`
      {
        orchestrationQueue {
          actionType
          parameters
        }
      }
    `
  },
  { excludeErrors: true }
)
  .filter(v => Boolean(v && v.orchestrationQueue))
  .map(v => v.orchestrationQueue);

interface AllBranchesQuery {
  allActualBranches: GitAutomationGQL.IGitRef[];
  configuredBranchGroups: (Pick<
    GitAutomationGQL.IBranchGroupDetails,
    "groupName" | "branchType" | "latestBranch" | "branches"
  > & {
    directDownstream: Pick<GitAutomationGQL.IBranchGroupDetails, "groupName">[];
  })[];
}

export const forceRefreshBranchGroups = new BehaviorSubject<null>(null);
export const allBranchGroups = forceRefreshBranchGroups
  .switchMap(() =>
    graphQl<AllBranchesQuery>(
      {
        query: gql`
          {
            allActualBranches {
              name
              commit
              url
              statuses {
                state
                description
                key
                url
              }
              badInfo {
                reasonCode
                timestamp
              }
              pullRequestsInto {
                id
                targetBranch
                url
                author
                isSystem
                reviews {
                  author
                  url
                  state
                }
                state
              }
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
              }
            }
          }
        `
      },
      { excludeErrors: true }
    )
  )
  .publishReplay(1)
  .refCount()
  .filter(v => Boolean(v && v.allActualBranches && v.configuredBranchGroups))
  .map((result): BranchGroupWithDownstream[] => {
    const configuredBranches = flatten<string>(
      result.configuredBranchGroups.map(g => g.branches.map(b => b.name))
    );
    const nonConfiguredBranches = result.allActualBranches.filter(
      b => configuredBranches.indexOf(b.name) === -1
    );
    return [
      ...result.configuredBranchGroups.map(group =>
        merge(group, {
          branches: group.branches.map(branch =>
            result.allActualBranches.find(b => b.name === branch.name)
          )
        })
      ),
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

export const allBranchesHierarchy = allBranchGroups.let(groupsToHierarchy);

export const branchDetails = (branchName: string) =>
  graphQl<Pick<GitAutomationGQL.IQuery, "branchGroup">>(
    {
      query: gql`
        query($branchName: String!) {
          branchGroup(name: $branchName) {
            groupName
            upstreamMergePolicy
            branchType
            directDownstream {
              groupName
            }
            directUpstream {
              groupName
            }
            latestBranch {
              name
              badInfo {
                reasonCode
                timestamp
              }
            }
            branches {
              name
              commit
              url
            }
          }
        }
      `,
      variables: {
        branchName
      }
    },
    { excludeErrors: true }
  )
    .filter(v => Boolean(v && v.branchGroup))
    .map(g => g.branchGroup);

export const detectUpstream = (branchName: string, asGroup: boolean) =>
  Observable.ajax(
    "/api/management/detect-upstream/" + branchName + `?asGroup=${asGroup}`
  ).map(response => response.response as string[]);

export const detectAllUpstream = (branchName: string) =>
  graphQl<Pick<GitAutomationGQL.IQuery, "allActualBranches">>(
    {
      query: gql`
        query($branchName: String!) {
          allActualBranches {
            name
            commit
            mergeBase(commitish: $branchName, kind: RemoteBranch)
          }
        }
      `,
      variables: {
        branchName
      }
    },
    { excludeErrors: true }
  )
    .filter(v => Boolean(v && v.allActualBranches))
    .map(g =>
      g.allActualBranches
        .filter(b => b.commit === b.mergeBase && b.name !== branchName)
        .map(b => b.name)
    );

export const checkPullRequests = (branchName: string) =>
  graphQl<Pick<GitAutomationGQL.IQuery, "branchGroup">>(
    {
      query: gql`
        query($branchName: String!) {
          branchGroup(name: $branchName) {
            latestBranch {
              pullRequestsFrom {
                id
                sourceBranch
                targetBranch
                state
                author
                url
                reviews {
                  author
                  state
                  url
                }
              }
            }
          }
        }
      `,
      variables: {
        branchName
      }
    },
    { excludeErrors: true }
  )
    .filter(v => Boolean(v && v.branchGroup && v.branchGroup.latestBranch))
    .map(g => g.branchGroup.latestBranch!.pullRequestsFrom);

export const forceRefreshLog = new BehaviorSubject<null>(null);
export const getLog = forceRefreshLog
  .switchMap(() =>
    graphQl<Pick<GitAutomationGQL.IQuery, "log">>(
      {
        query: gql`
          {
            log {
              __typename
              ... on StaticRepositoryActionEntry {
                isError
                message
              }
              ... on ProcessRepositoryActionEntry {
                startInfo
                output {
                  message
                  channel
                }
                state
                exitCode
              }
            }
          }
        `
      },
      { excludeErrors: true }
    )
  )
  .filter(v => Boolean(v && v.log))
  .map(g => g.log);

export const fetch = () =>
  Observable.ajax
    .post("/api/gitwebhook")
    .map(response => response.response as null);

export const createBranch = (
  branchName: string,
  body: {
    upstreamMergePolicy: GitAutomationGQL.IUpstreamMergePolicyEnum;
    branchType: GitAutomationGQL.IBranchGroupTypeEnum;
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
    upstreamMergePolicy: GitAutomationGQL.IUpstreamMergePolicyEnum;
    branchType: GitAutomationGQL.IBranchGroupTypeEnum;
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
  originalBranch
}: {
  targetBranch: string;
  originalBranch: string;
}) =>
  Observable.ajax
    .put("/api/management/branch/consolidate/" + targetBranch, originalBranch, {
      "Content-Type": "application/json"
    })
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
