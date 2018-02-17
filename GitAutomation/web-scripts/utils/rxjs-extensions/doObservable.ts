import { Observable } from "rxjs/Observable";

/** Splits the observable without altering it, except adding extra messages with
 * the same value. Used so that we can do things like this with the rxjs-d3
 * combo:
 *
 * ```
 * selection$
 *   .doObservable(
 *     sel$ =>
 *       sel$
 *         .map(fnSelect("ul"))
 *         .map(rxData(...))
 *   )
 * ```
 *
 * In the above example, the original `selection$` elements will be emitted as
 * normal. If additional elements are emitted from the inner observable, they
 * will be accumulated, but uses the latest element from the original.
 *  */
export function doObservable<T>(
  this: Observable<T>,
  fn: (target: Observable<T>) => Observable<any>
) {
  return this.publishReplay(1)
    .refCount()
    .let(obs =>
      new Observable<T>(observer => {
        return fn(obs)
          .catch(e => {
            if (process.env.ENV === "development") {
              console.error && console.error(e);
            }
            return Observable.empty<T>();
          })
          .subscribe(observer);
      }).switchMap(() => obs)
    );
}

export function doObservableNoChange<T>(
  this: Observable<T>,
  fn: (target: Observable<T>) => Observable<any>
) {
  return this.doObservable(v =>
    fn(v)
      .ignoreElements()
      .startWith(null)
  );
}

declare module "rxjs/Observable" {
  interface Observable<T> {
    doObservable: typeof doObservable;
    doObservableNoChange: typeof doObservableNoChange;
  }
}

Observable.prototype.doObservable = doObservable;
Observable.prototype.doObservableNoChange = doObservableNoChange;
