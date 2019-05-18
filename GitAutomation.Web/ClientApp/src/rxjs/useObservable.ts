import { Observable } from "rxjs";
import { useState, DependencyList } from "react";
import { useSubscription } from "./useSubscription";

export const useObservable = <T>(
  target: Observable<T>,
  defaultValue: T,
  deps?: DependencyList
) => {
  const [state, setState] = useState(defaultValue);
  useSubscription(() => target.subscribe(setState), deps || [target]);
  return state;
};
