import { Observable, Subscription } from "rxjs";
import { Selection } from "d3-selection";

import {
  rxData,
  rxEvent,
  fnSelect,
  rxDatum
} from "../utils/presentation/d3-binding";

import { RoutingComponent } from "../utils/routing-component";
import { ClaimDetails } from "../api/claim-details";

export const login = (
  container: Observable<Selection<HTMLElement, {}, null, undefined>>,
  claims: Observable<ClaimDetails>
): RoutingComponent => state =>
  container
    .do(elem =>
      elem.html(`
  <h1>Log In</h1>
  <p>You aren't currently logged in, or you've been granted no roles.</p>
  <a data-locator="log-in">Log In</a>

  <h1 data-locator="current-claims">Your current claims</h1>
  <ul data-locator="claims">
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
            target: body.map(fnSelect('[data-locator="log-in"]')),
            eventName: "click"
          }).subscribe(() => {
            window.location.href = "/api/authentication/sign-in";
          })
        );

        // display actions
        subscription.add(
          rxData(body, claims.map(claim => claim.claims))
            .bind<HTMLLIElement>({
              onCreate: target => target.append<HTMLLIElement>("li"),
              onEnter: li =>
                li.html(`
  <strong></strong> &mdash; <span></span>
`),
              selector: "li",
              onEach: selection => {
                selection.select(`strong`).text(data => data.type);
                selection.select(`span`).text(data => data.value);
              }
            })
            .subscribe()
        );

        rxDatum(claims.map(claim => Boolean(claim.claims.length)))(
          body.map(fnSelect('[data-locator="current-claims"]'))
        ).subscribe(target => {
          target.style(
            "display",
            hasClaims => (hasClaims ? "initial" : "none")
          );
        });

        return subscription;
      })
    );
