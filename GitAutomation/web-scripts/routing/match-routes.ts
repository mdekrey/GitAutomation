import { Routes, IRoutingState } from "./types";
import { parseRoutes, buildState } from "./operations";

export const matchRoutes = (routes: Routes) => {
  const parsed = parseRoutes(routes);

  return (state: IRoutingState) => buildState(parsed, state);
};
