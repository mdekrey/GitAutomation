import { Observable } from "rxjs";
import { OutputMessage } from "./output-message";

export const allBranches = () =>
  Observable.ajax("/api/management/all-branches").map(
    response => response.response as string[]
  );

export const branchDetails = (branchName: string) =>
  Observable.ajax("/api/management/details/" + branchName).map(
    response =>
      response.response as {
        recreateFromUpstream: boolean;
        branchName: string;
        directDownstreamBranches: string[];
        downstreamBranches: string[];
        directUpstreamBranches: string[];
        upstreamBranches: string[];
      }
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
