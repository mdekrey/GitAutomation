import { Observable } from "rxjs";
import { OutputMessage } from "./output-message";
import { BasicBranch } from "./basic-branch";
import { BranchDetails } from "./branch-details";

export const actionQueue = () =>
  Observable.ajax("/api/management/queue").map(
    response =>
      response.response as { actionType: string; parameters: string[] }[]
  );

export const allBranches = () =>
  Observable.ajax("/api/management/all-branches").map(
    response => response.response as BasicBranch[]
  );

export const branchDetails = (branchName: string) =>
  Observable.ajax("/api/management/details/" + branchName).map(
    response => response.response as BranchDetails
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
