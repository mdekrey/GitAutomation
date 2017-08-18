export const RouteTypeConcrete = "Concrete" as "Concrete";
export interface ConcreteRoute {
  type: typeof RouteTypeConcrete;
  data: any;
}
export function RouteConcrete(data: any): ConcreteRoute {
  return { type: RouteTypeConcrete, data };
}
export function isConcrete(
  route: any | null | undefined
): route is ConcreteRoute {
  return route && route.type === RouteTypeConcrete;
}
