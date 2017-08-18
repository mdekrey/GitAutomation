import { Route } from "./route-types/index";

export interface Routes {
  [key: string]: Route;
}

export interface IRoutingState {
  componentPath: string;
  routeVariables: { [name: string]: string };
  remainingPath: string | null;
  routeName: string | null;
  route: Route | null;
}
