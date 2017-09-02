import { Observable, Subscription } from "rxjs";
import { Selection } from "d3-selection";

import { rxData, rxEvent, fnSelect } from "../utils/presentation/d3-binding";

import { RoutingComponent } from "../utils/routing-component";
import {
  getLog,
  allBranches,
  fetch,
  actionQueue,
  signOut
} from "../api/basics";
import { logPresentation } from "../logs/log.presentation";
import { BasicBranch } from "../api/basic-branch";

export const homepage = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent => state =>
  container
    .do(elem =>
      elem.html(`
  <a data-locator="log-out">Log Out</a>

  <h1>Action Queue</h1>
  <a data-locator="action-queue-refresh">Refresh</a>
  <ul data-locator="action-queue">
  </ul>

  <h1>Remote Branches</h1>
  <a data-locator="remote-branches-refresh">Refresh</a>
  <a data-locator="fetch-from-remote">Fetch</a>
  <a data-locator="new-branch">New Branch</a>
  <ul data-locator="remote-branches">
  </ul>

  <h1>Current Status</h1>
  <a data-locator="status-refresh">Refresh</a>
  <ul data-locator="status">
  </ul>
`)
    )
    .publishReplay(1)
    .refCount()
    .let(body =>
      Observable.create(() => {
        const subscription = new Subscription();

        // log out
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
              .switchMap(() => actionQueue())
          )
            .bind<HTMLLIElement>({
              onCreate: target => target.append<HTMLLIElement>("li"),
              onEnter: li =>
                li.html(`
  <span></span>
`),
              selector: "li",
              onEach: selection =>
                selection.select(`span`).text(data => JSON.stringify(data))
            })
            .subscribe()
        );

        // display branches
        subscription.add(
          rxData<BasicBranch, HTMLUListElement>(
            body.map(fnSelect(`[data-locator="remote-branches"]`)),
            rxEvent({
              target: body.map(
                fnSelect('[data-locator="remote-branches-refresh"]')
              ),
              eventName: "click"
            })
              .startWith(null)
              .switchMap(() => allBranches())
          )
            .bind<HTMLLIElement>({
              onCreate: target => target.append<HTMLLIElement>("li"),
              onEnter: li =>
                li.html(`
  <span></span>
  <a data-locator="manage">Manage</a>
`),
              selector: "li",
              onEach: selection => {
                selection.select(`span`).text(data => data.branchName);
                subscription.add(
                  rxEvent({
                    target: Observable.of(
                      selection.select('[data-locator="manage"]')
                    ),
                    eventName: "click"
                  }).subscribe(event =>
                    state.navigate({
                      url: "/manage/" + event.datum.branchName,
                      replaceCurentHistory: false
                    })
                  )
                );
              }
            })
            .subscribe()
        );

        // display log
        subscription.add(
          rxData(
            body.map(fnSelect(`[data-locator="status"]`)),
            rxEvent({
              target: body.map(fnSelect('[data-locator="status-refresh"]')),
              eventName: "click"
            })
              .startWith(null)
              .switchMap(() => getLog())
          )
            .bind(logPresentation)
            .subscribe()
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
