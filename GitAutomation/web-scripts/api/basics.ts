import { Observable } from "rxjs";
import { OutputMessage } from "./output-message";
import { BasicBranch } from "./basic-branch";
import { BranchDetails } from "./branch-details";
import { ClaimDetails } from "./claim-details";
import { IUpdateUserRequestBody } from "./update-user";
import { BranchHierarchy } from "./branch-hierarchy";

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
    response => response.response as BasicBranch[]
  );

export const allBranchesHierarchy = () =>
  Observable.ajax("/api/management/all-branches/hierarchy").map(
    response => response.response as BranchHierarchy[]
  );

export const branchDetails = (branchName: string) =>
  Observable.ajax("/api/management/details/" + branchName).map(
    response => response.response as BranchDetails
  );

export const detectUpstream = (branchName: string) =>
  Observable.ajax("/api/management/detect-upstream/" + branchName).map(
    response => response.response as string[]
  );

export const getLog = () =>
  Observable.ajax("/api/management/log").map(
    response => response.response as OutputMessage[]
  );

export const fetch = () =>
  Observable.ajax
    .post("/api/gitwebhook")
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

export const promoteServiceLine = (body: {
  releaseCandidate: string;
  serviceLine: string;
  tagName: string;
}) =>
  Observable.ajax
    .put("/api/management/branch/promote", body, {
      "Content-Type": "application/json"
    })
    .map(response => response.response as null);

export const deleteBranch = (branchName: string) =>
  Observable.ajax
    .delete("/api/management/branch/" + branchName)
    .map(response => response.response as null);
