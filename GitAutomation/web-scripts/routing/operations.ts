import {
  fromPairs,
  mapObjIndexed,
  merge,
  sortBy,
  take,
  toPairs,
  values,
  zip
} from "ramda";
import { Route, isAlias } from "./route-types/index";
import { Routes, IRoutingState } from "./types";

export const buildPath = (componentPath: string | null) => (path: string) =>
  path[0] === "/"
    ? path
    : `/${[componentPath, path].filter(Boolean).map(trimSlashes).join("/")}`;

export function trimSlashes(path: string) {
  return path.replace(/^\/+/, "").replace(/\/+$/, "");
}

export interface IParsedRoute<T> {
  match: RegExp;
  // no such thing as named groups in JS RegExp - need to match varNames with order of arguments
  variables: string[];
  routeName: string;
  route: Route<T>;
}
export const wildcard = "**";

/** recognizes `:test`, `something/:test/something`, etc. */
const routeVariableRecognizer = /\:([a-zA-Z]+)($|\/)/g;
const execAll = (pattern: RegExp, target: string) => {
  const result: (string[] | null)[] = [];
  let current: string[] | null;
  do {
    current = pattern.exec(target);
    result.push(current);
  } while (current);
  return result.filter(Boolean).map(vals => vals![1]);
};

const parseRoute = <T>(routeName: string, route: Route<T>): IParsedRoute<T> =>
  routeName === wildcard
    ? {
        match: /.*/,
        variables: [],
        routeName,
        route
      }
    : {
        match: new RegExp(
          "^" +
            routeName.replace(
              routeVariableRecognizer,
              (_: string, _varName: string, endSlash: string) =>
                `([^/]+)${endSlash}`
            ) +
            "(/|$)"
        ),
        variables: execAll(routeVariableRecognizer, routeName),
        routeName,
        route
      };

const routePriority = (route: IParsedRoute<any>) => {
  const parts = route.routeName.split("/");
  return route.routeName === wildcard
    ? -1
    : parts.length + (1 - route.variables.length / (parts.length + 1));
};
const simplifyAliases = <T>(routes: Routes<T>) =>
  values(routes).reduce(
    innerRoutes =>
      mapObjIndexed(
        (v: Route<T>) => (isAlias(v) ? innerRoutes[v.alias] : v),
        innerRoutes
      ),
    routes
  );
const prioritizeRoutes = <T>(routes: IParsedRoute<T>[]) =>
  sortBy(a => -routePriority(a), routes);
export const parseRoutes = <T>(routes: Routes<T>): IParsedRoute<T>[] =>
  prioritizeRoutes(
    toPairs<string, Route<T>>(
      simplifyAliases(routes)
    ).map(([routeName, route]) => parseRoute(routeName, route))
  );

export function buildState<T>(
  parsedRoutes: IParsedRoute<T>[],
  routing: IRoutingState<any>
): IRoutingState<T> {
  const { componentPath, remainingPath, routeVariables } = routing;
  const found = parsedRoutes
    .map(route => ({ route, matchExp: route.match.exec(remainingPath!)! }))
    .find(e => Boolean(e.matchExp));
  if (!found) {
    return {
      routeName: null,
      route: null,
      remainingPath: null,
      componentPath,
      routeVariables
    };
  }
  const { route: { routeName, variables, route }, matchExp } = found;

  const matchedPath = trimSlashes(matchExp[0]);
  const localVariables = fromPairs(
    zip(variables, take(variables.length, matchExp.slice(1)))
  );
  return {
    routeName,
    route,
    remainingPath: trimSlashes(remainingPath!.substr(matchedPath.length)),
    componentPath: [componentPath, matchedPath].filter(a => a).join("/"),
    routeVariables: merge(routeVariables, localVariables)
  };
}

export function buildDefaultState(remainingPath: string): IRoutingState<never> {
  return {
    remainingPath: trimSlashes(remainingPath),
    componentPath: "",
    routeVariables: {},
    routeName: null,
    route: null
  };
}
