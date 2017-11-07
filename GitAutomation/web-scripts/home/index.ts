import { Observable, Subscription } from "../utils/rxjs";
import { Selection } from "d3-selection";

import {
  bind,
  rxData,
  rxEvent,
  fnSelect
} from "../utils/presentation/d3-binding";

import { RoutingComponent } from "../utils/routing-component";
import {
  getLog,
  allBranchGroups,
  fetch,
  actionQueue,
  signOut,
  allBranchesHierarchy,
  forceRefreshBranchGroups,
  forceRefreshLog
} from "../api/basics";
import { logPresentation } from "../logs/log.presentation";
import { branchHierarchy } from "./branch-hierarchy";
import { branchNameDisplay } from "../branch-name-display";

export const standardMenu = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent<any> => state =>
  container
    .do(elem => elem.html(require("./menu.layout.html")))
    .publishReplay(1)
    .refCount()
    .let(body =>
      Observable.create(() => {
        const subscription = new Subscription();

        subscription.add(
          rxEvent({
            target: body.map(fnSelect('[data-locator="home"]')),
            eventName: "click"
          }).subscribe(() =>
            state.navigate({ url: "/", replaceCurentHistory: false })
          )
        );

        subscription.add(
          rxEvent({
            target: body.map(fnSelect('[data-locator="log-out"]')),
            eventName: "click"
          })
            .switchMap(signOut)
            .subscribe(() => {
              window.location.href = "/";
            })
        );

        subscription.add(
          rxEvent({
            target: body.map(fnSelect('[data-locator="admin"]')),
            eventName: "click"
          }).subscribe(() =>
            state.navigate({ url: "/admin", replaceCurentHistory: false })
          )
        );

        subscription.add(
          rxEvent({
            target: body.map(fnSelect('[data-locator="auto-wireup"]')),
            eventName: "click"
          }).subscribe(() =>
            state.navigate({
              url: "/auto-wireup",
              replaceCurentHistory: false
            })
          )
        );

        return subscription;
      })
    );

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
          rxData<
            Pick<
              GitAutomationGQL.IBranchGroupDetails,
              "groupName" | "branches"
            >,
            HTMLUListElement
          >(
            body.map(fnSelect(`[data-locator="remote-branches"]`)),
            allBranchGroups
          )
            .bind<HTMLLIElement>({
              onCreate: target =>
                target
                  .append<HTMLLIElement>("li")
                  .attr("data-locator", "remote-branch"),
              onEnter: li => li.html(require("./home.branch-group.html")),
              selector: `li[data-locator="remote-branch"]`,
              onEach: selection => {
                branchNameDisplay(
                  selection.select('[data-locator="name-container"]')
                );
                subscription.add(
                  rxEvent({
                    target: Observable.of(
                      selection.select('[data-locator="manage"]')
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
            .let(configuredBranches =>
              configuredBranches
                .map(branch =>
                  branch
                    .select(`ul[data-locator="actual-branches"]`)
                    .selectAll(`li`)
                    .data(basicBranch => basicBranch.branches)
                )
                .map(target =>
                  bind({
                    target,
                    onCreate: target => target.append("li"),
                    onEach: target =>
                      target.text(
                        data => `${data.name} (${data.commit.substr(0, 7)})`
                      )
                  })
                )
            )
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

        subscription.add(
          rxEvent({
            target: body.map(fnSelect('[data-locator="new-branch"]')),
            eventName: "click"
          }).subscribe(() =>
            state.navigate({ url: "/new-branch", replaceCurentHistory: false })
          )
        );

        return subscription;
      })
    );
