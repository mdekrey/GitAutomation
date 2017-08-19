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

const body = Observable.of(document.body);

function selectChildren<T extends Element>(query: string) {
  return (children: Observable<Element>) =>
    children
      .map(within => within.querySelector(query) as T | null)
      .filter(Boolean)
      .distinctUntilChanged()
      .map(d3element);
}

rxEvent({
  target: body.let(selectChildren('[data-locator="fetch-from-remote"]')),
  eventName: "click"
})
  .switchMap(() => fetch())
  .subscribe();

rxData<string, HTMLUListElement>(
  body.let(
    selectChildren<HTMLUListElement>(`[data-locator="remote-branches"]`)
  ),
  rxEvent({
    target: body.let(
      selectChildren('[data-locator="remote-branches-refresh"]')
    ),
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
  body.let(selectChildren<HTMLUListElement>(`[data-locator="status"]`)),
  rxEvent({
    target: body.let(selectChildren('[data-locator="status-refresh"]')),
    eventName: "click"
  })
    .startWith(null)
    .switchMap(() => getLog())
).bind(logPresentation);

buildCascadingStrategy(windowHashStrategy)
  .let(
    route<string>({
      "": RouteConcrete("home"),
      manage: RouteConcrete("manage"),
      admin: RouteAlias("manage"),
      [wildcard]: RouteConcrete("wildcard")
    })
  )
  .map(routeState => {
    return routeState;
  })
  .subscribe(_ => {
    (window as any).currentRouteState = _;
    console.log(_.state);
  });
