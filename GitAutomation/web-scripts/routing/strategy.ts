import { Observable } from "rxjs";
import { IRoutingState, Routes } from "./types";
import { matchRoutes } from "./match-routes";
import { buildPath } from "./operations";

export type RoutingNavigate = (
  args: { url: string; replaceCurentHistory: boolean }
) => void;

export interface IRoutingStrategy {
  state: Observable<IRoutingState>;
  navigate: RoutingNavigate;
}

export function buildStrategy(
  state: Observable<IRoutingState>,
  navigate: RoutingNavigate
): IRoutingStrategy {
  return { state, navigate };
}

export interface ICascadingRoutingStrategy {
  state: IRoutingState;
  navigate: RoutingNavigate;
}

export function buildCascadingStrategy(
  strategy: IRoutingStrategy
): Observable<ICascadingRoutingStrategy> {
  return strategy.state.map(state => ({
    state,
    navigate: strategy.navigate
  }));
}

export function route(routes: Routes) {
  const parsed = matchRoutes(routes);
  return (strategy: Observable<ICascadingRoutingStrategy>) =>
    strategy.map((current): ICascadingRoutingStrategy => {
      const state = parsed(current.state);
      return {
        state,
        navigate: ({ url, replaceCurentHistory }) =>
          current.navigate({
            url: buildPath(state.componentPath)(url),
            replaceCurentHistory
          })
      };
    });
}
