import { Observable, Subscription } from "../utils/rxjs";
import { Selection } from "d3-selection";

import { rxData, fnSelect, fnEvent } from "../utils/presentation/d3-binding";

import { RoutingComponent } from "../utils/routing-component";
import { getLog, actionQueue, forceRefreshLog } from "../api/basics";
import { logPresentation } from "../logs/log.presentation";

// import { secured } from "../security/security-binding";
import { handleError } from "../handle-error";

export const debugPage = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent<never> => state =>
  container
    .do(elem => elem.html(require("./debug.layout.html")))
    .publishReplay(1)
    .refCount()
    // .let(secured)
    .let(body =>
      Observable.create(() => {
        const subscription = new Subscription();

        // display actions
        subscription.add(
          rxData(
            body.map(
              fnSelect<HTMLUListElement>(`[data-locator="action-queue"]`)
            ),
            body
              .map(fnSelect('[data-locator="action-queue-refresh"]'))
              .let(fnEvent("click"))
              .startWith(null)
              .switchMap(() => actionQueue)
              .let(handleError)
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

        // display log
        subscription.add(
          rxData(
            body.map(fnSelect(`ul[data-locator="status"]`)),
            getLog,
            (_, index) => index
          )
            .bind(logPresentation)
            .subscribe()
        );

        subscription.add(
          body
            .map(fnSelect('[data-locator="status-refresh"]'))
            .let(fnEvent("click"))
            .subscribe(() => forceRefreshLog.next(null))
        );

        return subscription;
      })
    );
