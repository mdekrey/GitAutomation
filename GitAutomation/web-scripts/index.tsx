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
import { Homepage } from "./home/home";
import { debugPage } from "./debug/index";
import { manage } from "./manage-branch/index";
import { newBranch } from "./new-branch/index";
import { Login } from "./login/login";
import { Admin } from "./admin/admin";
import { AutoWireup } from "./setup-wizard/auto-wireup";
import { RxD3 } from "./utils/rxjs-d3-component";

const injector = new Injector(
  new ProviderBuilder<InjectedServices>().apply(routeProviders).build()
);

const Scaffolded = (child: React.Props<any>["children"]) =>
  RouteConcrete(<Scaffolding menu={<StandardMenu />}>{child}</Scaffolding>);

const baseRoutes: ComponentRoutes = {
  login: RouteConcrete(<Login />),

  "": Scaffolded(<Homepage />),
  manage: Scaffolded(<RxD3 do={manage} />),
  "new-branch": Scaffolded(<RxD3 do={newBranch} />),
  "auto-wireup": Scaffolded(<AutoWireup />),
  admin: Scaffolded(<Admin />),
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
  document.body.appendChild(document.createElement("div"))
);
