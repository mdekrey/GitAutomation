/// <reference path="./custom.d.ts" />
import { Observable } from "./utils/rxjs";
import { d3element } from "./utils/presentation/d3-binding";
import {
  buildCascadingStrategy,
  ICascadingRoutingStrategy,
  route,
  RouteConcrete,
  wildcard
} from "./routing";
import { windowHashStrategy } from "./routing/strategies/window-hash";
import { RoutingComponent, renderRouteOnce } from "./utils/routing-component";
import { homepage, standardMenu } from "./home/index";
import { manage } from "./manage-branch/index";
import { newBranch } from "./new-branch/index";
import { currentClaims } from "./api/basics";
import { login } from "./login/index";
import "./style/global";
import { admin } from "./admin/index";
import { setupWizard } from "./setup-wizard/index";
import { scaffolding } from "./layout/scaffolding";

const body = Observable.of(d3element(document.body));
const bodyWithScaffolding = body.let(scaffolding);
const scaffoldingContents = bodyWithScaffolding.map(b => b.contents);
const scaffoldingMenu = bodyWithScaffolding.map(b => b.menu);

const claims = currentClaims.publishReplay(1);
claims.connect();

const withSecurity = buildCascadingStrategy(windowHashStrategy).let(
  handleSecurity
);

withSecurity
  .map(
    route<RoutingComponent<never>>({
      "": RouteConcrete(homepage(scaffoldingContents)),
      manage: RouteConcrete(manage(scaffoldingContents)),
      "new-branch": RouteConcrete(newBranch(scaffoldingContents)),
      "auto-wireup": RouteConcrete(setupWizard(scaffoldingContents)),
      admin: RouteConcrete(admin(scaffoldingContents)),
      login: RouteConcrete(login(body, claims)),
      [wildcard]: RouteConcrete(() =>
        body
          .do(elem => elem.html(`Four-oh-four`))
          .switchMap(v => Observable.empty<never>())
      )
    })
  )
  .switchMap(renderRouteOnce)
  .subscribe({
    error: ex => {
      console.error(ex);
    }
  });

withSecurity
  .map(
    route<RoutingComponent<any>>({
      login: RouteConcrete(() => Observable.empty()),
      [wildcard]: RouteConcrete(standardMenu(scaffoldingMenu))
    })
  )
  .switchMap(renderRouteOnce)
  .subscribe({
    error: ex => {
      console.error(ex);
    }
  });

function handleSecurity(strategy: Observable<ICascadingRoutingStrategy<any>>) {
  interface SecurityComponent {
    redirectPath: string | null;
    state: ICascadingRoutingStrategy<any>;
  }
  return strategy
    .map(
      route<RoutingComponent<SecurityComponent>>({
        login: RouteConcrete<RoutingComponent<SecurityComponent>>(state =>
          claims.map(claims => ({
            redirectPath: claims.roles.length !== 0 ? "/" : null,
            state: state.parent!
          }))
        ),
        [wildcard]: RouteConcrete<RoutingComponent<SecurityComponent>>(state =>
          claims.map(claims => ({
            redirectPath: claims.roles.length === 0 ? "/login" : null,
            state: state.parent!
          }))
        )
      })
    )
    .switchMap(renderRouteOnce)
    .do(v => {
      if (v.redirectPath) {
        v.state.navigate({
          url: v.redirectPath,
          replaceCurentHistory: true
        });
      }
    })
    .filter(v => !v.redirectPath)
    .map(v => v.state);
}
