import { Injector } from "./Injector";
import { Resolvers } from "./Resolvers";
import { Resolver } from "./Resolver";

export class InjectorBuilder<TServices extends {}, TScope extends string> {
  private readonly scopeHierarchy: TScope[];
  private readonly serviceResolvers: Partial<Resolvers<TServices>>;
  private readonly scopeByService: Partial<
    Record<keyof TServices, TScope | null>
  >;

  constructor(
    scopeHierarchy: TScope[],
    serviceResolvers: Partial<Resolvers<TServices>> = {},
    scopeByService: Partial<Record<keyof TServices, TScope | null>> = {}
  ) {
    this.scopeHierarchy = scopeHierarchy;
    this.serviceResolvers = serviceResolvers;
    this.scopeByService = scopeByService;
  }

  set<TService extends keyof TServices>(
    service: TService,
    scope: TScope | null,
    resolver: Resolver<TServices, TServices[TService]>
  ) {
    this.serviceResolvers[service] = resolver;
    this.scopeByService[service] = scope;
    return this;
  }

  getResolver<TService extends keyof TServices>(service: TService) {
    return this.serviceResolvers[service];
  }

  getScope<TService extends keyof TServices>(service: TService) {
    return this.scopeByService[service];
  }

  copy() {
    return new InjectorBuilder(
      this.scopeHierarchy,
      { ...this.serviceResolvers },
      { ...this.scopeByService }
    );
  }

  build(): Injector<TServices, TScope> {
    return new Injector(
      this.serviceResolvers as Resolvers<TServices>,
      this.scopeByService as Record<keyof TServices, TScope | null>,
      this.scopeHierarchy
    );
  }
}
