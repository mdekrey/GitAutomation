import { Observable } from "rxjs";

import { ICascadingRoutingStrategy } from "../routing/index";

export type RoutingComponent = (
  state: ICascadingRoutingStrategy<any>
) => Observable<any>;

export const renderRoute = (
  target: Observable<ICascadingRoutingStrategy<RoutingComponent>>
) =>
  target.switchMap(routeState => {
    const { route } = routeState.state;
    if (route) {
      return route.data(routeState);
    }
    return Observable.empty();
  });
