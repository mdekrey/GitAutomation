import { useContext, useMemo, DependencyList } from "react";
import { InjectedServices } from "./InjectedServices";
import { injectorContext } from "./injectorContext";

export const useService = <TService extends keyof InjectedServices>(
  service: TService,
  deps?: DependencyList
) => {
  const injector = useContext(injectorContext);
  return useMemo(
    () => injector.resolve(service),
    // eslint-disable-next-line
    deps || [injector, service]
  );
};
