import { Observable } from "./rxjs";

import { ICascadingRoutingStrategy } from "../routing/index";

export type RoutingComponent<T> = (
  state: ICascadingRoutingStrategy<any>
) => Observable<T>;

export const renderRouteOnce = <T>(
  routeState: ICascadingRoutingStrategy<RoutingComponent<T>>
) => {
  const { route } = routeState.state;
  if (route) {
    return route.data(routeState);
  }
  return Observable.empty<T>();
};

export const renderRoute = <T>(
  target: Observable<ICascadingRoutingStrategy<RoutingComponent<T>>>
) => target.switchMap(renderRouteOnce);
