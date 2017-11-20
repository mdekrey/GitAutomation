import { Observable, Subscription } from "../utils/rxjs";
import { Selection } from "d3-selection";

import { rxData, fnSelect, fnEvent } from "../utils/presentation/d3-binding";

import { RoutingComponent } from "../utils/routing-component";
import {
  allBranchGroups,
  fetch,
  allBranchesHierarchy,
  forceRefreshBranchGroups
} from "../api/basics";
import { branchHierarchy } from "./branch-hierarchy";
import { flatten, sortBy } from "../utils/ramda";

import { style } from "typestyle";
import { branchNameDisplay } from "../branch-name-display";
import { secured } from "../security/security-binding";

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

export const homepage = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent<never> => state =>
  container
    .do(elem => elem.html(require("./home.layout.html")))
    .publishReplay(1)
    .refCount()
    .let(secured)
    .let(body =>
      Observable.create(() => {
        const subscription = new Subscription();
        console.log("here");
        subscription.add(() => {
          console.log("unsubscribe");
        });

        subscription.add(
          branchHierarchy({
            target: body.map(
              fnSelect<SVGSVGElement>(`svg[data-locator="hierarchy-container"]`)
            ),
            navigate: state.navigate,
            data: allBranchesHierarchy
          })
            .do(
              v => console.log(v),
              err => console.log(err),
              () => console.log("completed?")
            )
            .subscribe()
        );

        subscription.add(
          body
            .map(fnSelect('[data-locator="remote-branch-hierarchy-refresh"]'))
            .let(fnEvent("click"))
            .subscribe(v => forceRefreshBranchGroups.next(null))
        );

        // fetch from remote
        subscription.add(
          body
            .map(fnSelect('[data-locator="fetch-from-remote"]'))
            .let(fnEvent("click"))
            .switchMap(() => fetch())
            .subscribe()
        );

        // display branches
        subscription.add(
          rxData(
            body
              .map(
                fnSelect<HTMLTableElement>(`[data-locator="remote-branches"]`)
              )
              .do(e => e.classed(remoteBranchesTableLayout, true)),
            allBranchGroups.map(groups =>
              flatten(
                sortBy(group => group.groupName, groups).map(group => {
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
          )
            .bind<HTMLTableRowElement>({
              onCreate: target =>
                target
                  .append<HTMLTableRowElement>("tr")
                  .attr("data-locator", "remote-branch"),
              onEnter: tr => tr.html(require("./home.branch-group.html")),
              selector: `tr[data-locator="remote-branch"]`,
              onEach: selection => {
                selection.classed(
                  groupTopRow,
                  group =>
                    group.branch === null ||
                    group.branch.name === group.branches[0].name
                );
                branchNameDisplay(
                  selection
                    .select('[data-locator="name-container"]')
                    .attr("rowspan", group =>
                      Math.max(1, group.branches.length).toFixed(0)
                    )
                    .style(
                      "display",
                      group =>
                        group.branch === null ||
                        group.branch.name === group.branches[0].name
                          ? null
                          : "none"
                    ),
                  state.navigate
                );
                selection
                  .select('[data-locator="actual-branch"]')
                  .text(
                    ({ branch: data }) =>
                      data
                        ? `${data.name} (${data.commit.substr(0, 7)})`
                        : "(Branch not created)"
                  );
                subscription.add(
                  Observable.of(
                    selection
                      .select('[data-locator="name-container"]')
                      .style(
                        "display",
                        group =>
                          group.branch === null ||
                          group.branch.name === group.branches[0].name
                            ? null
                            : "none"
                      )
                  )
                    .let(fnEvent("click"))
                    .subscribe(event =>
                      state.navigate({
                        url: "/manage/" + event.datum.groupName,
                        replaceCurentHistory: false
                      })
                    )
                );
              }
            })
            .subscribe()
        );

        subscription.add(
          body
            .map(fnSelect('[data-locator="remote-branches-refresh"]'))
            .let(fnEvent("click"))
            .subscribe(v => forceRefreshBranchGroups.next(null))
        );

        return subscription;
      })
    );
