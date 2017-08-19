import { AliasRoute } from "./alias";
import { ConcreteRoute } from "./concrete";

export type Route<T> = AliasRoute | ConcreteRoute<T>;

export { RouteAlias, isAlias } from "./alias";
export { RouteConcrete, isConcrete } from "./concrete";
