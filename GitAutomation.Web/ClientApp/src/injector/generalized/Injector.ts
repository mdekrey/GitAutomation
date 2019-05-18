import { Resolvers } from "./Resolvers";

export class Injector<TServices extends {}, TScope extends string> {
  private readonly serviceResolvers: Resolvers<TServices>;
  private readonly scopeByService: Record<keyof TServices, TScope | null>;
  private readonly scopedResolutions: {
    [scopeLevel in TScope]?: Partial<TServices>
  };
  private readonly scopeHierarchy: TScope[];

  constructor(
    serviceResolvers: Resolvers<TServices>,
    scopeByService: Record<keyof TServices, TScope | null>,
    scopeHierarchy: TScope[],
    scopedResolutions: { [scopeLevel in TScope]?: Partial<TServices> } = {}
  ) {
    this.serviceResolvers = serviceResolvers;
    this.scopeByService = scopeByService;
    this.scopeHierarchy = scopeHierarchy;
    this.scopedResolutions = scopedResolutions;
  }

  beginScope(scope: TScope) {
    this.scopedResolutions[scope] = {};
  }
  endScope(scope: TScope) {
    delete this.scopedResolutions[scope];
  }

  createChildInjector() {
    const parent = this.scopedResolutions;
    const scopedResolutions: typeof parent = {};
    Object.setPrototypeOf(scopedResolutions, parent);
    return new Injector(
      this.serviceResolvers,
      this.scopeByService,
      this.scopeHierarchy,
      scopedResolutions
    );
  }

  resolve<TService extends keyof TServices>(
    service: TService,
    inputMinScope?: TScope
  ): TServices[TService] {
    const minScope =
      inputMinScope === undefined
        ? this.scopeHierarchy[this.scopeHierarchy.length - 1]
        : inputMinScope;
    const minScopeLevel = this.scopeHierarchy.indexOf(minScope);
    for (const scope of this.scopeHierarchy) {
      if (this.scopeHierarchy[minScopeLevel + 1] === scope) {
        // extended out of scope
        break;
      }
      const scopedResolutions = this.scopedResolutions[scope];
      if (scopedResolutions) {
        if (service in scopedResolutions) {
          return scopedResolutions[service]!;
        }
      }
    }

    const targetScope = this.scopeByService[service];
    const targetScopeLevel = this.getScopeLevel(targetScope);
    if (targetScopeLevel !== null && targetScopeLevel > minScopeLevel) {
      throw new Error(
        `Target scope '${targetScope}' is more specific than allowed '${minScope}' for service '${service}'.`
      );
    }
    if (targetScope !== null && !this.scopedResolutions[targetScope!]) {
      throw new Error(
        `Target scope '${targetScope}' has not been started for service '${service}'.`
      );
    }
    const nextScope =
      targetScopeLevel !== null && targetScopeLevel < minScopeLevel
        ? targetScope!
        : minScope;
    try {
      const result = this.serviceResolvers[service](newService =>
        this.resolve(newService, nextScope)
      );
      if (targetScope !== null) {
        const scopedResolutions: Partial<TServices> = this.scopedResolutions[
          targetScope!
        ]!;
        scopedResolutions[service] = result;
      }
      return result;
    } catch (err) {
      throw new Error(
        `When resolving Service '${service}' in Scope '${targetScope}'...\n  ${err}`
      );
    }
  }

  private getScopeLevel(scope: TScope | null): number | null {
    if (scope === null) {
      return null;
    }
    const scopeLevel = this.scopeHierarchy.indexOf(scope);
    if (scopeLevel === -1) {
      throw new Error(
        `Scope '${scope}' not in hierarchy '${this.scopeHierarchy}'`
      );
    }
    return scopeLevel;
  }
}
