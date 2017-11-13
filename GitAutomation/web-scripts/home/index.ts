import { Observable, Subscription } from "../utils/rxjs";
import { Selection } from "d3-selection";
import { branchTypeColors } from "../style/branch-colors";

import { rxData, rxEvent, fnSelect } from "../utils/presentation/d3-binding";

import { RoutingComponent } from "../utils/routing-component";
import {
  getLog,
  allBranchGroups,
  fetch,
  actionQueue,
  allBranchesHierarchy,
  forceRefreshBranchGroups,
  forceRefreshLog
} from "../api/basics";
import { logPresentation } from "../logs/log.presentation";
import { branchHierarchy } from "./branch-hierarchy";
import { flatten, sortBy } from "../utils/ramda";

import { style } from "typestyle";

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
    .let(body =>
      Observable.create(() => {
        const subscription = new Subscription();

        subscription.add(
          branchHierarchy({
            target: body.map(
              fnSelect<SVGSVGElement>(`svg[data-locator="hierarchy-container"]`)
            ),
            navigate: state.navigate,
            data: allBranchesHierarchy
          }).subscribe()
        );

        subscription.add(
          rxEvent({
            target: body.map(
              fnSelect('[data-locator="remote-branch-hierarchy-refresh"]')
            ),
            eventName: "click"
          }).subscribe(v => forceRefreshBranchGroups.next(null))
        );

        // fetch from remote
        subscription.add(
          rxEvent({
            target: body.map(fnSelect('[data-locator="fetch-from-remote"]')),
            eventName: "click"
          })
            .switchMap(() => fetch())
            .subscribe()
        );

        // display actions
        subscription.add(
          rxData(
            body.map(
              fnSelect<HTMLUListElement>(`[data-locator="action-queue"]`)
            ),
            rxEvent({
              target: body.map(
                fnSelect('[data-locator="action-queue-refresh"]')
              ),
              eventName: "click"
            })
              .startWith(null)
              .switchMap(() => actionQueue)
          )
            .bind<HTMLLIElement>({
              onCreate: target => target.append<HTMLLIElement>("li"),
              onEnter: li => li.append("span"),
              selector: "li",
              onEach: selection =>
                selection.select(`span`).text(data => JSON.stringify(data))
            })
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
                  )
                  .style("color", group =>
                    branchTypeColors[group.branchType][0].toHexString()
                  )
                  .text(group => group.groupName);
                selection
                  .select('[data-locator="actual-branch"]')
                  .text(
                    ({ branch: data }) =>
                      data
                        ? `${data.name} (${data.commit.substr(0, 7)})`
                        : "(Branch not created)"
                  );
                subscription.add(
                  rxEvent({
                    target: Observable.of(
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
                    ),
                    eventName: "click"
                  }).subscribe(event =>
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
          rxEvent({
            target: body.map(
              fnSelect('[data-locator="remote-branches-refresh"]')
            ),
            eventName: "click"
          }).subscribe(v => forceRefreshBranchGroups.next(null))
        );

        // display log
        subscription.add(
          rxData(
            body.map(fnSelect(`ul[data-locator="status"]`)),
            getLog.catch(() =>
              Observable.empty<GitAutomationGQL.IOutputMessage[]>()
            )
          )
            .bind(logPresentation)
            .subscribe()
        );

        subscription.add(
          rxEvent({
            target: body.map(fnSelect('[data-locator="status-refresh"]')),
            eventName: "click"
          }).subscribe(() => forceRefreshLog.next(null))
        );

        return subscription;
      })
    );
