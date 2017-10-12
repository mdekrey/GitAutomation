import { Observable, Subscription } from "../utils/rxjs";
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
  <div data-locator="not-logged-in">
    <h1>Log In</h1>
    <p>You aren't currently logged in.</p>
    <a data-locator="log-in">Log In</a>
  </div>

  <div data-locator="current-claims">
    <h1>Your current claims</h1>
    <p>If you're seeing this screen, share the below values with your administrator so they can give you access.</p>
    <ul data-locator="claims">
    </ul>
  </div>
`)
    )
    .publishReplay(1)
    .refCount()
    .let(body =>
      Observable.create(() => {
        const subscription = new Subscription();

        // begin the sign-in process
        subscription.add(
          rxEvent({
            target: body.map(fnSelect('[data-locator="log-in"]')),
            eventName: "click"
          }).subscribe(() => {
            window.location.href = "/api/authentication/sign-in";
          })
        );

        // display claims
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

        const hasClaims = claims.map(claim => Boolean(claim.claims.length));
        rxDatum(hasClaims)(
          body.map(fnSelect('[data-locator="current-claims"]'))
        ).subscribe(target => {
          target.style("display", hasClaims => (hasClaims ? null : "none"));
        });

        rxDatum(hasClaims)(
          body.map(fnSelect('[data-locator="not-logged-in"]'))
        ).subscribe(target => {
          target.style("display", hasClaims => (hasClaims ? "none" : null));
        });

        return subscription;
      })
    );
