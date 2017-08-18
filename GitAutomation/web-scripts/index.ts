import { Observable } from "rxjs";
import { rxData, rxEvent, d3element } from "./utils/presentation/d3-binding";
import { getLog, remoteBranches, fetch } from "./api/basics";
import { logPresentation } from "./logs/log.presentation";
import {
  buildCascadingStrategy,
  route,
  RouteConcrete,
  RouteAlias,
  wildcard
} from "./routing";
import { windowHashStrategy } from "./routing/strategies/window-hash";

const domChanged = Observable.of(null);

function watchElements<T extends Element>(query: string) {
  return domChanged
    .map(() => document.querySelector(query) as T | null)
    .filter(Boolean)
    .distinctUntilChanged()
    .map(d3element);
}

rxEvent({
  target: watchElements('[data-locator="fetch-from-remote"]'),
  eventName: "click"
})
  .switchMap(() => fetch())
  .subscribe();

rxData<string, HTMLUListElement>(
  watchElements<HTMLUListElement>(`[data-locator="remote-branches"]`),
  rxEvent({
    target: watchElements('[data-locator="remote-branches-refresh"]'),
    eventName: "click"
  })
    .startWith(null)
    .switchMap(() => remoteBranches())
).bind<HTMLLIElement>({
  onCreate: target => target.append<HTMLLIElement>("li"),
  selector: "li",
  onEach: selection => {
    selection.text(data => data);
  }
});

rxData(
  watchElements<HTMLUListElement>(`[data-locator="status"]`),
  rxEvent({
    target: watchElements('[data-locator="status-refresh"]'),
    eventName: "click"
  })
    .startWith(null)
    .switchMap(() => getLog())
).bind(logPresentation);

buildCascadingStrategy(windowHashStrategy)
  .let(
    route({
      [wildcard]: RouteConcrete("wildcard"),
      manage: RouteConcrete("manage"),
      admin: RouteAlias("manage")
    })
  )
  .subscribe(_ => {
    (window as any).currentRouteState = _;
    console.log(_.state);
  });
