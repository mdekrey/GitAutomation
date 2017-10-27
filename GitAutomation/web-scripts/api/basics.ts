import gql from "graphql-tag";
import { Observable } from "../utils/rxjs";
import { OutputMessage } from "./output-message";
import { BranchGroup } from "./basic-branch";
import { ClaimDetails } from "./claim-details";
import { IUpdateUserRequestBody } from "./update-user";
import { graphQl } from "./graphql";
import { flatten } from "../utils/ramda";

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
    pollInterval: 60000
  });

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
  }).map(r =>
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
  }).map(r => r.roles);

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

type AllBranchesQuery = {
  allActualBranches: GitAutomationGQL.IGitRef[];
  configuredBranchGroups: Pick<
    GitAutomationGQL.IBranchGroupDetails,
    "groupName" | "branchType" | "latestBranch" | "branches"
  >[];
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
  }).map(result => {
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
        branches: [branch]
      }))
    ];
  });

export const allBranchesHierarchy = () =>
  Observable.ajax("/api/management/all-branches/hierarchy").map(
    response => response.response as BranchGroup[]
  );

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
