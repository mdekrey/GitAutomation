import { Observable, Subject, combineLatest } from "rxjs";
import { switchAll } from "rxjs/operators";
import { useEffect, useMemo } from "react";

export const useSubscription = <T>(
  observable: Observable<T>,
  callback: (data: T) => void
) => {
  const observable$ = useMemo(() => new Subject<Observable<T>>(), []);
  const observer$ = useMemo(() => new Subject<(data: T) => void>(), []);

  useEffect(() => {
    const subscription = combineLatest(
      observable$.pipe(switchAll()),
      observer$
    ).subscribe(([value, observer]) => observer(value));
    return () => subscription.unsubscribe();
  }, [observable$, observer$]);

  useEffect(() => {
    observer$.next(callback);
  }, [callback, observer$]);

  useEffect(() => {
    observable$.next(observable);
  }, [observable, observable$]);
};
