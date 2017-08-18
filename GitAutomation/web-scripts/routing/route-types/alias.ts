export const RouteTypeAlias = "alias" as "alias";
export interface AliasRoute {
  type: typeof RouteTypeAlias;
  alias: string;
}
export function RouteAlias(alias: string): AliasRoute {
  return { type: RouteTypeAlias, alias };
}

export function isAlias(route: any | null | undefined): route is AliasRoute {
  return route && route.type === RouteTypeAlias;
}
