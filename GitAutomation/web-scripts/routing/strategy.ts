import { Observable } from "rxjs";
import { IRoutingState, Routes } from "./types";
import { matchRoutes } from "./match-routes";
import { buildPath } from "./operations";

export type RoutingNavigate = (
  args: { url: string; replaceCurentHistory: boolean }
) => void;

export interface IRoutingStrategy<T> {
  state: Observable<IRoutingState<T>>;
  navigate: RoutingNavigate;
}

export function buildStrategy(
  state: Observable<IRoutingState<never>>,
  navigate: RoutingNavigate
): IRoutingStrategy<never> {
  return { state, navigate };
}

export interface ICascadingRoutingStrategy<T> {
  state: IRoutingState<T>;
  navigate: RoutingNavigate;
}

export function buildCascadingStrategy(
  strategy: IRoutingStrategy<never>
): Observable<ICascadingRoutingStrategy<never>> {
  return strategy.state.map(state => ({
    state,
    navigate: strategy.navigate
  }));
}

export function route<T>(routes: Routes<T>) {
  const parsed = matchRoutes(routes);
  return (strategy: Observable<ICascadingRoutingStrategy<any>>) =>
    strategy.map((current): ICascadingRoutingStrategy<T> => {
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
