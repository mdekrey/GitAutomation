import { Observable } from "../utils/rxjs";

import {
  ICascadingRoutingStrategy,
  route,
  RouteConcrete,
  wildcard
} from "../routing";
import { RoutingComponent, renderRouteOnce } from "../utils/routing-component";
import { equals } from "../utils/ramda";
import { currentClaims } from "../api/basics";

export const claims = currentClaims.publishReplay(1);
claims.connect();

interface SecurityComponent {
  redirectPath: string | null;
  state: ICascadingRoutingStrategy<any>;
}

export function handleSecurity(
  strategy: Observable<ICascadingRoutingStrategy<any>>
) {
  return strategy
    .map(
      route<RoutingComponent<SecurityComponent>>({
        login: RouteConcrete<RoutingComponent<SecurityComponent>>(state =>
          claims.map(claims => ({
            redirectPath: claims.roles.length !== 0 ? "/" : null,
            state: state.parent!
          }))
        ),
        [wildcard]: RouteConcrete<RoutingComponent<SecurityComponent>>(state =>
          claims.map(claims => ({
            redirectPath: claims.roles.length === 0 ? "/login" : null,
            state: state.parent!
          }))
        )
      })
    )
    .switchMap(renderRouteOnce)
    .do(v => {
      if (v.redirectPath) {
        v.state.navigate({
          url: v.redirectPath,
          replaceCurentHistory: true
        });
      }
    })
    .filter(v => !v.redirectPath)
    .map(v => v.state)
    .distinctUntilChanged((a, b) => equals(a, b));
}
