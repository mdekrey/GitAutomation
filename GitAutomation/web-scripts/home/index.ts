import { Observable, Subscription } from "rxjs";
import { Selection } from "d3-selection";

import {
  rxData,
  rxEvent,
  selectChildren
} from "../utils/presentation/d3-binding";

import { RoutingComponent } from "../utils/routing-component";
import { getLog, remoteBranches, fetch } from "../api/basics";
import { logPresentation } from "../logs/log.presentation";

export const homepage = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent => state =>
  container
    .do(elem =>
      elem.html(`
  <h1>Action Queue</h1>
  <ul data-locator="action-queue">
  </ul>

  <h1>Remote Branches</h1>
  <a data-locator="remote-branches-refresh">Refresh</a>
  <a data-locator="remote-branches-remove">Remove</a>
  <a data-locator="fetch-from-remote">Fetch</a>
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

        // fetch from remote
        subscription.add(
          rxEvent({
            target: body.let(
              selectChildren('[data-locator="fetch-from-remote"]')
            ),
            eventName: "click"
          })
            .switchMap(() => fetch())
            .subscribe()
        );

        // display branches
        subscription.add(
          rxData<string, HTMLUListElement>(
            body.let(selectChildren(`[data-locator="remote-branches"]`)),
            rxEvent({
              target: body.let(
                selectChildren('[data-locator="remote-branches-refresh"]')
              ),
              eventName: "click"
            })
              .startWith(null)
              .switchMap(() => remoteBranches())
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
                selection.select(`span`).text(data => data);
                subscription.add(
                  rxEvent({
                    target: Observable.of(
                      selection.select('[data-locator="manage"]')
                    ),
                    eventName: "click"
                  }).subscribe(event =>
                    state.navigate({
                      url: "/manage/" + event.datum,
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
            body.let(selectChildren(`[data-locator="status"]`)),
            rxEvent({
              target: body.let(
                selectChildren('[data-locator="status-refresh"]')
              ),
              eventName: "click"
            })
              .startWith(null)
              .switchMap(() => getLog())
          )
            .bind(logPresentation)
            .subscribe()
        );

        return subscription;
      })
    );
