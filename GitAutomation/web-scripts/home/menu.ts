import { Observable, Subscription } from "../utils/rxjs";
import { Selection } from "d3-selection";

import { rxEvent, fnSelect } from "../utils/presentation/d3-binding";

import { RoutingComponent } from "../utils/routing-component";
import { signOut } from "../api/basics";

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
