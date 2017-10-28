import { Route } from "./route-types/index";

export type Routes<T> = Record<string, Route<T>>;

export interface IRoutingState<T> {
  componentPath: string;
  routeVariables: { [name: string]: string };
  remainingPath: string | null;
  routeName: string | null;
  route: Route<T> | null;
}
