import { Observable } from "rxjs";
import { useState } from "react";
import { useSubscription } from "./useSubscription";

export const useObservable = <T>(target: Observable<T>, defaultValue: T) => {
  const [state, setState] = useState(defaultValue);
  useSubscription(target, setState);
  return state;
};
