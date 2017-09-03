import { Routes, IRoutingState } from "./types";
import { parseRoutes, buildState } from "./operations";

export const matchRoutes = <T>(routes: Routes<T>) => {
  const parsed = parseRoutes(routes);

  return (state: IRoutingState<any>) => buildState(parsed, state);
};
