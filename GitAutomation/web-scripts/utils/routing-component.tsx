import * as React from "react";
import {
  RoutingNavigate as ImportedRoutingNavigate,
  IConcreteRoutingState,
  ConcreteRoute
} from "@woosti/rxjs-router";
import { Injector, InjectorCleanup, Provider } from "./injection-registrations";
import {
  ICascadingRoutingStrategy,
  buildCascadingStrategy
} from "@woosti/rxjs-router";
import { Observable } from "./rxjs";
import { windowHashStrategy } from "@woosti/rxjs-router/lib/strategies/window-hash";
import { Routes, route } from "@woosti/rxjs-router/lib";
import { noop } from "./noop";
import { identity } from "./ramda";
// import { LoadingBlockIfDelayed } from "common/loading";

const LoadingBlock = (props: { className?: string; key?: string }) => (
  // TODO - make better
  <div className={props.className} key={props.key}>
    Loading...
  </div>
);

export const LoadingBlockIfDelayed = (
  component: Observable<RenderableElement>
) => component.imperceptible(LoadingBlock);

export type RenderableElement = JSX.Element | null;
export type ElementFactory = (
  injector: Injector
) => Observable<RenderableElement>;
export type ObservableElement = ElementFactory | RenderableElement;
export type RoutingNavigate = ImportedRoutingNavigate;
export type RouteState = IConcreteRoutingState<ObservableElement>;
export type RouteHrefBuilder = (path: string) => string;
export type RoutingStrategy = Observable<
  ICascadingRoutingStrategy<never | ObservableElement>
>;
export type ComponentRoutes = Routes<ObservableElement>;

const isObservableFactory = (
  maybeObservable: any
): maybeObservable is ElementFactory => typeof maybeObservable === "function";

export const routeProviders: Provider = builder =>
  builder.addValue(
    "routingStrategy",
    buildCascadingStrategy(windowHashStrategy)
  );

declare module "./injection-registrations" {
  interface InjectedServices {
    routingStrategy: RoutingStrategy;
  }
}

export const perRouteScope = (p: Provider): Provider => builder =>
  builder.apply(p).addMultiValue("routeScopedProviders", p);

export const addRouteScopedCleanup = (
  p: InjectorCleanup
): Provider => builder => builder.addMultiValue("routeScopedCleanup", p);

declare module "./injection-registrations" {
  interface InjectedServices {
    routeNavigate: RoutingNavigate;
    routeHrefBuilder: RouteHrefBuilder;
    routeScopedProviders: Provider[];
    routeScopedCleanup: InjectorCleanup[];
  }
}

export interface IInjectorProviderProps {
  injector: Injector;
  content: RenderableElement;
  cleanup?: InjectorCleanup;
}
export class InjectorProvider extends React.Component<
  IInjectorProviderProps,
  never
> {
  static childContextTypes = { injector: () => null };
  getChildContext(): InjectionContext {
    return { injector: this.props.injector };
  }

  componentWillReceiveProps(nextProps: IInjectorProviderProps) {
    if (nextProps.injector !== this.props.injector) {
      console.error(
        "Injector changing without unmounting. This could result in cleanup not being called!"
      );
    }
    if (nextProps.cleanup !== this.props.cleanup) {
      console.error(
        "Cleanup changing without unmounting. This could result in cleanup not being called!"
      );
    }
  }

  componentWillUnmount(): void {
    if (this.props.cleanup) {
      this.props.cleanup(this.props.injector.services);
    }
  }

  render() {
    return this.props.content;
  }
}

export interface InjectionContext {
  injector: Injector;
}

export abstract class ContextComponent<
  TProps = {},
  TState = never
> extends React.PureComponent<TProps, TState> {
  context: InjectionContext;
  static contextTypes = { injector: () => null };
}

export interface IRouterProps {
  routes: ComponentRoutes;
  providers?: Provider;
  cleanup?: InjectorCleanup;
  loading?: (
    component: Observable<RenderableElement>
  ) => Observable<RenderableElement>;
}

const noopBuilder: Provider = identity;

export class Router extends ContextComponent<IRouterProps> {
  render() {
    const {
      providers = noopBuilder,
      cleanup = noop,
      loading = LoadingBlockIfDelayed
    } = this.props;
    const resultStrategy = this.context.injector.services.routingStrategy
      .map(route(this.props.routes))
      .publishReplay(1)
      .refCount();

    const resultCleanup: InjectorCleanup = injector => {
      const cleansers = injector.routeScopedCleanup;
      if (cleansers) {
        cleansers.forEach(cleanse => cleanse(injector));
      }
      cleanup(injector);
    };
    const scopedProviders =
      this.context.injector.services.routeScopedProviders || [];
    return resultStrategy
      .distinctUntilChanged(
        (x, y) => x.state.componentPath === y.state.componentPath
      )
      .map(currentStrategy => {
        const resultProviders: Provider = builder =>
          scopedProviders
            .reduce((b, next) => b.apply(next), builder)
            .apply(providers)
            .addValue(
              "routingStrategy",
              resultStrategy.takeWhile(
                v =>
                  v.state.componentPath === currentStrategy.state.componentPath
              )
            )
            .addValue("routeNavigate", a => currentStrategy.navigate(a))
            .addValue("routeHrefBuilder", p => currentStrategy.pathToLink(p));
        const childInjector = this.context.injector.childContainer(
          resultProviders
        );
        const routeData =
          currentStrategy.state.route && currentStrategy.state.route.data;
        const observableData = isObservableFactory(routeData)
          ? routeData(childInjector)
          : Observable.of(routeData);
        return observableData
          .let(loading)
          .map(elem => (
            <InjectorProvider
              key={currentStrategy.state.componentPath}
              injector={childInjector}
              cleanup={resultCleanup}
              content={elem}
            />
          ));
      })
      .switch()
      .asComponent();
  }
}

export function RoutePromiseFactory<T>(
  importer: () => Promise<T>,
  componentFactory: (arg: T) => ElementFactory
) {
  return ConcreteRoute<ObservableElement>(injector =>
    Observable.of(null)
      .map(() => Observable.fromPromise(importer()))
      .switch()
      .map(module => componentFactory(module))
      .switchMap(factory => factory(injector))
  );
}

function RenderableRedirect({
  path,
  routeNavigate
}: {
  path: string;
  routeNavigate: RoutingNavigate;
}) {
  routeNavigate({
    url: path,
    replaceCurentHistory: true
  });
  return null;
}

export function RedirectRoute(path: string) {
  return ConcreteRoute<ObservableElement>(injector => {
    return Observable.of(
      <RenderableRedirect
        path={path}
        routeNavigate={injector.services.routeNavigate}
      />
    );
  });
}

export type RoutingComponent<T> = (
  state: ICascadingRoutingStrategy<any>
) => Observable<T>;

export function renderRouteOnce<T>(
  routeState: ICascadingRoutingStrategy<RoutingComponent<T>>
) {
  const { route } = routeState.state;
  if (route) {
    return route.data(routeState);
  }
  return Observable.empty<T>();
}

export function renderRoute<T>(
  target: Observable<ICascadingRoutingStrategy<RoutingComponent<T>>>
) {
  return target.switchMap(renderRouteOnce);
}
