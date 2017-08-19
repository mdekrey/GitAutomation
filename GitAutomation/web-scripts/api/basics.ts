import { Observable } from "rxjs";
import { OutputMessage } from "./output-message";

export const remoteBranches = () =>
  Observable.ajax("/api/management/remote-branches").map(
    response => response.response as string[]
  );

export const downstreamBranches = (branchName: string) =>
  Observable.ajax("/api/management/downstream-branches/" + branchName).map(
    response => response.response as string[]
  );

export const upstreamBranches = (branchName: string) =>
  Observable.ajax("/api/management/upstream-branches/" + branchName).map(
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
    addUpstream: string[];
    addDownstream: string[];
    removeUpstream: string[];
    removeDownstream: string[];
  }
) =>
  Observable.ajax
    .put("/api/management/branch/" + branchName, body, {
      "Content-Type": "application/json"
    })
    .map(response => response.response as null);
