import * as React from "react";
import { Observable } from "../utils/rxjs";

import { ContextComponent } from "../utils/routing-component";
import {
  allBranchGroups,
  fetch,
  allBranchesHierarchy,
  forceRefreshBranchGroups
} from "../api/basics";
import { branchHierarchy } from "./branch-hierarchy";
import { flatten } from "../utils/ramda";

import { style } from "typestyle";
import { handleError } from "../handle-error";
import { RxD3 } from "../utils/rxjs-d3-component";
import { BranchNameDisplay } from "../branch-name-display";
import { sortBranches } from "../utils/branch-sorting";

const remoteBranchesTableLayout = style({
  borderCollapse: "collapse",
  $nest: {
    th: {
      textAlign: "end",
      verticalAlign: "top"
    }
  }
});
const groupTopRow = style({
  $nest: {
    "> *:before": {
      content: "' '",
      display: "block",
      height: "0.25em"
    }
  }
});

export class Homepage extends ContextComponent<{}> {
  render() {
    return (
      <>
        <h1>Branches</h1>
        <p>
          <a onClick={this.refreshBranches}>Refresh</a>
        </p>
        <RxD3
          do={target => () =>
            branchHierarchy({
              target: target as Observable<any>,
              navigate: this.handleNavigate,
              data: allBranchesHierarchy
            })}
        >
          <svg width="800" height="200" style={{ maxHeight: "70vh" }} />
        </RxD3>
        <h1>Remote Branches</h1>
        <a onClick={this.refreshBranches}>Refresh</a>{" "}
        <a onClick={this.fetchFromRemote}>Fetch</a>
        <table>
          <thead>
            <tr>
              <th style={{ textAlign: "end" }}>Group Name</th>
              <th style={{ textAlign: "start" }}>Actual Branches</th>
            </tr>
          </thead>
          <tbody className={remoteBranchesTableLayout}>
            {allBranchGroups
              .map(groups =>
                flatten(
                  sortBranches(groups).map(group => {
                    const atLeastOneRow: (GitAutomationGQL.IGitRef | null)[] =
                      group.branches.length > 0 ? group.branches : [null];
                    return atLeastOneRow.map(
                      (branch: GitAutomationGQL.IGitRef | null) => ({
                        branch: branch,
                        ...group
                      })
                    );
                  })
                )
              )
              .map(groups => (
                <>
                  {groups.map(group =>
                    (group.branches.length
                      ? group.branches
                      : ([null] as (GitAutomationGQL.IGitRef | null)[])
                    ).map((branch, index) => (
                      <tr
                        key={branch ? branch.name : group.groupName}
                        className={index === 0 ? groupTopRow : ""}
                      >
                        <th
                          style={{ display: index === 0 ? undefined : "none" }}
                          rowSpan={Math.max(group.branches.length, 1)}
                        >
                          <BranchNameDisplay branch={group} />
                        </th>
                        {branch ? (
                          <td>
                            {branch.name} ({branch.commit.substr(0, 7)})
                          </td>
                        ) : (
                          <td>(Branch not created)</td>
                        )}
                      </tr>
                    ))
                  )}
                </>
              ))
              .asComponent()}
          </tbody>
        </table>
      </>
    );
  }

  refreshBranches = () => forceRefreshBranchGroups.next(null);
  fetchFromRemote = () =>
    fetch()
      .let(handleError)
      .subscribe();
  handleNavigate = this.context.injector.services.routeNavigate;
}
