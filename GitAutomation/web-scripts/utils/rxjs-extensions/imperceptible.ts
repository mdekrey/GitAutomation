import { Observable } from "rxjs/Observable";

const imperceptibleObservable = Observable.interval(140).take(1);

export function imperceptibleDelay<T>(
  this: Observable<T>,
  delayIf: (value: T) => boolean
) {
  return this.switchMap(
    v => (delayIf(v) ? imperceptibleObservable.map(() => v) : Observable.of(v))
  );
}

/** Introduces an extra frame after the "imperceptible" amount of time to
 * indicate system delay. This allows for the system to appear faster even while
 * loading is happening, preventing some loading screens.
 */
export function imperceptible<T>(
  this: Observable<T>,
  defaultFactory: (props: {}) => T
) {
  return this.publishReplay(1)
    .refCount()
    .let(obs =>
      imperceptibleObservable
        .map(defaultFactory)
        .takeUntil(obs)
        .merge(obs)
    );
}

declare module "rxjs/Observable" {
  interface Observable<T> {
    imperceptibleDelay: typeof imperceptibleDelay;
    imperceptible: typeof imperceptible;
  }
}

Observable.prototype.imperceptibleDelay = imperceptibleDelay;
Observable.prototype.imperceptible = imperceptible;
