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
import { currentClaims } from "./api/basics";
import { ClaimDetails } from "./api/claim-details";
import { login } from "./login/index";

const body = Observable.of(d3element(document.body));

const claims = currentClaims().publishReplay(1);
claims.connect();

buildCascadingStrategy(windowHashStrategy)
  .let(
    route<RoutingComponent>({
      "": RouteConcrete(homepage(body)),
      manage: RouteConcrete(manage(body)),
      "new-branch": RouteConcrete(newBranch(body)),
      admin: RouteAlias("manage"),
      login: RouteConcrete(login(body, claims)),
      [wildcard]: RouteConcrete(() =>
        body.do(elem => elem.html(`Four-oh-four`))
      )
    })
  )
  .let(renderRoute)
  .subscribe({
    error: ex => {
      console.error(ex);
    }
  });

buildCascadingStrategy(windowHashStrategy)
  .let(
    route<RoutingComponent>({
      login: RouteConcrete(() =>
        claims
          .filter<ClaimDetails>(claims => claims.roles.length !== 0)
          .map(() => "/")
      ),
      [wildcard]: RouteConcrete(() =>
        claims
          .filter<ClaimDetails>(claims => claims.roles.length === 0)
          .map(() => "/login")
      )
    })
  )
  .let(renderRoute)
  .subscribe((url: string) =>
    windowHashStrategy.navigate({ url, replaceCurentHistory: true })
  );
