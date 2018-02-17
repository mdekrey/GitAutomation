import {
  InjectionContainer,
  ProviderBuildup,
  InjectionFactory
} from "./injection";

export interface InjectedServices {
  injector: InjectionContainer<InjectedServices>;
}
export type Provider = ProviderBuildup<InjectedServices>;
export interface Injectable<T> {
  inject: InjectionFactory<InjectedServices, T>;
}
export type InjectorCleanup = ((injector: InjectedServices) => void);

export type Injector = InjectionContainer<InjectedServices>;
export const Injector = InjectionContainer;
