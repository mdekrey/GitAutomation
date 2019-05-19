import { ApiService } from "./ApiService";
import { UnreservedBranchesService } from "./UnreservedBranchesService";
import { injectorBuilder, Scope } from "../injector";

declare module "../injector/InjectedServices" {
  interface InjectedServices {
    api: ApiService;
    unreservedBranches: UnreservedBranchesService;
  }
}

injectorBuilder.set("api", Scope.Singleton, () => new ApiService());
injectorBuilder.set(
  "unreservedBranches",
  Scope.Singleton,
  resolver => new UnreservedBranchesService(resolver("api"))
);

export * from "./ApiService";
export * from "./BranchReserve";
export * from "./BranchReserveBranch";
export * from "./ReserveConfiguration";
export * from "./UpstreamReserve";
export * from "./UnreservedBranchesService";
