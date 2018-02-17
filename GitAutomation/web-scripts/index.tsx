/// <reference path="./custom.d.ts" />
import * as React from "react";
import * as ReactDOM from "react-dom";
import {
  InjectorProvider,
  routeProviders,
  ComponentRoutes,
  Router
} from "./utils/routing-component";
import { Injector, InjectedServices } from "./utils/injection-registrations";
import { ProviderBuilder } from "./utils/injection";
import { RouteConcrete, wildcard } from "./routing";
import "./style/global";
import { Scaffolding } from "./layout/scaffolding";
import { StandardMenu } from "./home/menu";
import { RouteSecurity } from "./security/app-access";
import { homepage } from "./home/index";
import { debugPage } from "./debug/index";
import { manage } from "./manage-branch/index";
import { newBranch } from "./new-branch/index";
import { login } from "./login/index";
import { admin } from "./admin/index";
import { setupWizard } from "./setup-wizard/index";
import { RxD3 } from "./utils/rxjs-d3-component";

const injector = new Injector(
  new ProviderBuilder<InjectedServices>().apply(routeProviders).build()
);

const Scaffolded = (child: React.Props<any>["children"]) =>
  RouteConcrete(<Scaffolding menu={<StandardMenu />}>{child}</Scaffolding>);

const baseRoutes: ComponentRoutes = {
  login: RouteConcrete(<RxD3 do={login} />),

  "": Scaffolded(<RxD3 do={homepage} />),
  manage: Scaffolded(<RxD3 do={manage} />),
  "new-branch": Scaffolded(<RxD3 do={newBranch} />),
  "auto-wireup": Scaffolded(<RxD3 do={setupWizard} />),
  admin: Scaffolded(<RxD3 do={admin} />),
  debug: Scaffolded(<RxD3 do={debugPage} />),
  [wildcard]: Scaffolded("Four-oh-four")
};

ReactDOM.render(
  <InjectorProvider
    injector={injector}
    content={
      <>
        <RouteSecurity />
        <Router routes={baseRoutes} />
      </>
    }
  />,
  document.body
);
