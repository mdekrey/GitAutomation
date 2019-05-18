import { InjectorBuilder } from "./generalized/InjectorBuilder";
import { InjectedServices } from "./InjectedServices";
import { Scope } from "./Scope";

export const injectorBuilder = new InjectorBuilder<InjectedServices, Scope>([
  Scope.Singleton,
  Scope.Component,
]);
