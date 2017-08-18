import { AliasRoute } from "./alias";
import { ConcreteRoute } from "./concrete";

export type Route = AliasRoute | ConcreteRoute;

export { RouteAlias, isAlias } from "./alias";
export { RouteConcrete, isConcrete } from "./concrete";
