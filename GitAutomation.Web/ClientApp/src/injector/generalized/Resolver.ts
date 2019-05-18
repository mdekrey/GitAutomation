import { GenericResolver } from "./GenericResolver";

export type Resolver<TServices extends {}, TTarget> = (
  services: GenericResolver<TServices>
) => TTarget;
