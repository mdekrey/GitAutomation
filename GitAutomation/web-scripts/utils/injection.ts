export type InjectionFactory<TServices, T> = (
  injector: InjectionContainer<TServices>
) => T;

export type Injectable<TServices, T> =
  | { new (): T }
  | { inject: InjectionFactory<TServices, T> };

export interface ValueProvider<TValue> {
  type: "value";
  value: TValue;
}
export interface FactoryProvider<TInput, TValue> {
  type: "factory";
  factory: InjectionFactory<TInput, TValue>;
}

export type AnyProvider<T, TValue> =
  | ValueProvider<TValue>
  | FactoryProvider<T, TValue>;

export type GenericProvider<T> = AnyProvider<T, T[keyof T]>;
export type ProviderSet<T> = { [P in keyof T]?: AnyProvider<T, T[P]> };

function hasInjectFunction(
  injectable: Injectable<any, any>
): injectable is { inject: InjectionFactory<any, any> } {
  return Boolean((injectable as any).inject);
}

const injectableToFactory = <TServices, T>(
  injectable: Injectable<TServices, T>
) =>
  hasInjectFunction(injectable) ? injectable.inject : () => new injectable();

export class ProviderBuilder<T> {
  constructor(private providers: ProviderSet<T> = {}) {}

  apply(p: (builder: ProviderBuilder<T>) => ProviderBuilder<T>) {
    p(this);
    return this;
  }

  addValue<K extends keyof T>(key: K, value: T[K]): this {
    this.providers[key] = { value, type: "value" };
    return this;
  }

  addFactory<K extends keyof T>(
    key: K,
    factory: InjectionFactory<T, T[K]>
  ): this {
    this.providers[key] = { factory, type: "factory" };
    return this;
  }

  addInjectable<K extends keyof T>(
    key: K,
    injectable: Injectable<T, T[K]>
  ): this {
    return this.addFactory(key, injectableToFactory(injectable));
  }

  addMulti<K extends keyof T, TValue, TArray extends Array<TValue> & T[K]>(
    key: K,
    provider: FactoryProvider<T, TValue> | ValueProvider<TValue>
  ): this {
    const original = this.providers[key];
    const incomingProvider = this.toFactory(provider).factory;
    if (original) {
      const originalFactory = this.toFactory(original!).factory;
      this.providers[key] = {
        type: "factory",
        factory: injected =>
          [
            ...(originalFactory(injected) as TArray),
            incomingProvider(injected)
          ] as TArray
      };
    } else {
      this.providers[key] = {
        type: "factory",
        factory: injected => [incomingProvider(injected)] as TArray
      };
    }
    return this;
  }

  addMultiValue<K extends keyof T, TValue, TArray extends Array<TValue> & T[K]>(
    key: K,
    value: TValue
  ): this {
    return this.addMulti<K, TValue, TArray>(key, { type: "value", value });
  }

  addMultiFactory<
    K extends keyof T,
    TValue,
    TArray extends Array<TValue> & T[K]
  >(key: K, factory: InjectionFactory<T, TValue>): this {
    return this.addMulti<K, TValue, TArray>(key, { type: "factory", factory });
  }

  build(): ProviderSet<T> {
    return this.providers;
  }

  private toFactory<TValue>(
    provider: AnyProvider<T, TValue>
  ): FactoryProvider<T, TValue> {
    if (provider.type === "factory") {
      return provider;
    }
    return {
      type: "factory",
      factory: _ => provider.value
    };
  }
}
export type ProviderBuildup<T> = (
  builder: ProviderBuilder<T>
) => ProviderBuilder<T>;

export class InjectionContainer<T> {
  private readonly cache: { [P in keyof T]?: { value: T[P] } } = {};
  protected readonly definedKeys: ReadonlyArray<keyof T>;

  constructor(providers: ProviderSet<T>);
  constructor(providers: ProviderBuildup<T>, parent: InjectionContainer<T>);
  constructor(
    providers: ProviderSet<T> | (ProviderBuildup<T>),
    parent: InjectionContainer<T> | null = null
  ) {
    if (parent && typeof providers === "function") {
      const inheritedKeys = parent.definedKeys;

      providers = providers(
        inheritedKeys.reduce(
          (builder, key) => builder.addFactory(key, () => parent.services[key]),
          new ProviderBuilder<T>()
        )
      ).build();
    }
    if (typeof providers !== "function") {
      const providerSet: ProviderSet<T> = providers;
      this.definedKeys = Object.keys(providerSet) as (keyof T)[];
      this.services = Object.defineProperty(
        this.definedKeys.reduce(
          (target, key) => {
            const provider = providerSet[key]!;
            return Object.defineProperty(
              target,
              key,
              provider.type == "value"
                ? this.provideValue(provider)
                : this.provideFactory(key, provider)
            );
          },
          ({} as any) as T
        ),
        "injector",
        {
          get: () => this
        }
      );
    }
  }

  private provideValue(provider: ValueProvider<any>): PropertyDescriptor {
    return {
      get: () => provider.value
    };
  }

  private provideFactory(
    key: keyof T,
    provider: FactoryProvider<T, any>
  ): PropertyDescriptor {
    return {
      get: () => {
        const cached = this.cache[key];
        if (cached) {
          return cached.value;
        }

        const result = provider.factory(this);
        this.cache[key] = { value: result };
        return result;
      }
    };
  }

  public readonly services: T;
  public childContainer(providers: ProviderBuildup<T>): InjectionContainer<T> {
    return new InjectionContainer<T>(providers, this);
  }

  public build<TResult>(injectable: Injectable<T, TResult>) {
    return injectableToFactory(injectable)(this);
  }
}
