import { Observable, Subscription } from "../utils/rxjs";
import { Selection } from "d3-selection";

import { fnEvent, fnSelect } from "../utils/presentation/d3-binding";

import { RoutingComponent } from "../utils/routing-component";
import { signOut } from "../api/basics";
import { secured } from "../security/security-binding";

export const standardMenu = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>
): RoutingComponent<any> => state =>
  container
    .do(elem => elem.html(require("./menu.layout.html")))
    .publishReplay(1)
    .refCount()
    .let(secured)
    .let(body =>
      Observable.create(() => {
        const subscription = new Subscription();

        subscription.add(
          body
            .map(fnSelect('[data-locator="home"]'))
            .let(fnEvent("click"))
            .subscribe(() =>
              state.navigate({ url: "/", replaceCurentHistory: false })
            )
        );

        subscription.add(
          body
            .map(fnSelect('[data-locator="log-out"]'))
            .let(fnEvent("click"))
            .switchMap(signOut)
            .subscribe(() => {
              window.location.href = "/";
            })
        );

        subscription.add(
          body
            .map(fnSelect('[data-locator="admin"]'))
            .let(fnEvent("click"))
            .subscribe(() =>
              state.navigate({ url: "/admin", replaceCurentHistory: false })
            )
        );

        subscription.add(
          body
            .map(fnSelect('[data-locator="auto-wireup"]'))
            .let(fnEvent("click"))
            .subscribe(() =>
              state.navigate({
                url: "/auto-wireup",
                replaceCurentHistory: false
              })
            )
        );

        subscription.add(
          body
            .map(fnSelect('[data-locator="new-branch"]'))
            .let(fnEvent("click"))
            .subscribe(() =>
              state.navigate({
                url: "/new-branch",
                replaceCurentHistory: false
              })
            )
        );

        return subscription;
      })
    );
