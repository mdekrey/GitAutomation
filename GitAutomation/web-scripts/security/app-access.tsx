import * as React from "react";
import { Observable } from "../utils/rxjs";

import {
  ICascadingRoutingStrategy,
  route,
  ConcreteRoute,
  wildcard
} from "@woosti/rxjs-router";
import {
  RoutingComponent,
  renderRouteOnce,
  ContextComponent
} from "../utils/routing-component";
import { equals } from "../utils/ramda";
import { currentClaims } from "../api/basics";
import { Subscription } from "rxjs/Subscription";

export const claims = currentClaims.publishReplay(1);
claims.connect();

interface SecurityComponent {
  redirectPath: string | null;
  state: ICascadingRoutingStrategy<any>;
}

export class RouteSecurity extends ContextComponent {
  private subscription: Subscription;

  componentDidMount() {
    this.subscription = this.context.injector.services.routingStrategy
      .let(handleSecurity)
      .subscribe();
  }

  componentWillUnmount() {
    this.subscription.unsubscribe();
  }

  render() {
    return <>{this.props.children || null}</>;
  }
}

function handleSecurity(strategy: Observable<ICascadingRoutingStrategy<any>>) {
  return strategy
    .map(
      route<RoutingComponent<SecurityComponent>>({
        login: ConcreteRoute<RoutingComponent<SecurityComponent>>(state =>
          claims.map(claims => ({
            redirectPath: claims.roles.length !== 0 ? "/" : null,
            state: state.parent!
          }))
        ),
        [wildcard]: ConcreteRoute<RoutingComponent<SecurityComponent>>(state =>
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
    .map(v => v.state)
    .distinctUntilChanged((a, b) => equals(a, b));
}
