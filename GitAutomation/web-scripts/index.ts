import { Observable } from "rxjs";
import { d3element } from "./utils/presentation/d3-binding";
import {
  buildCascadingStrategy,
  route,
  RouteConcrete,
  RouteAlias,
  wildcard
} from "./routing";
import { windowHashStrategy } from "./routing/strategies/window-hash";
import { RoutingComponent, renderRoute } from "./utils/routing-component";
import { homepage } from "./home/index";
import { manage } from "./manage-branch/index";
import { newBranch } from "./new-branch/index";
import "./style/global";

const body = Observable.of(d3element(document.body));

buildCascadingStrategy(windowHashStrategy)
  .let(
    route<RoutingComponent>({
      "": RouteConcrete(homepage(body)),
      manage: RouteConcrete(manage(body)),
      "new-branch": RouteConcrete(newBranch(body)),
      admin: RouteAlias("manage"),
      [wildcard]: RouteConcrete(() => Observable.empty())
    })
  )
  .let(renderRoute)
  .subscribe({
    next: _ => {
      // (window as any).currentRouteState = _;
      console.log(_);
    },
    error: ex => {
      console.error(ex);
    }
  });
