import { ApiService } from "./ApiService";
import { injectorBuilder, Scope } from "../injector";

declare module "../injector/InjectedServices" {
  interface InjectedServices {
    api: ApiService;
  }
}

injectorBuilder.set("api", Scope.Singleton, () => new ApiService());

export * from "./ApiService";
export * from "./BranchReserve";
export * from "./BranchReserveBranch";
export * from "./ReserveConfiguration";
export * from "./UpstreamReserve";
