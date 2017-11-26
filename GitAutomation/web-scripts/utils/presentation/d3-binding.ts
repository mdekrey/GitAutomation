import { Observable, Observer, Subject } from "../rxjs";
import {
  event as d3event,
  mouse as d3mouse,
  select as d3select,
  Selection,
  BaseType,
  EnterElement
} from "d3-selection";

export function d3element<GElement extends BaseType>(element: GElement) {
  return d3select(element);
}

export interface IBindBaseProps<
  GElement extends BaseType,
  NewDatum,
  PElement extends BaseType,
  PDatum
> {
  /** The element to be created per datum */
  onCreate: (
    target: Selection<EnterElement, NewDatum, PElement, PDatum>
  ) => Selection<GElement, NewDatum, PElement, PDatum>;
  /** Sets up the newly created child elements */
  onEnter?: (
    target: Selection<GElement, NewDatum, PElement, PDatum>,
    parent: Selection<GElement, NewDatum, PElement, PDatum>
  ) => void;

  /**
   * Optional. How to handle data being removed. By default, it calls `.remove()`.
   */
  onExit?: (target: Selection<GElement, NewDatum, PElement, PDatum>) => void;

  /**
   * Updates elements to handle the new data.
   */
  onEach: (target: Selection<GElement, NewDatum, PElement, PDatum>) => void;
}

export interface IBindProps<
  GElement extends BaseType,
  NewDatum,
  PElement extends BaseType,
  PDatum
> extends IBindBaseProps<GElement, NewDatum, PElement, PDatum> {
  target: Selection<GElement, NewDatum, PElement, PDatum>;
}

export function bind<GElement extends BaseType, NewDatum>({
  target,
  onCreate,
  onEnter,
  onExit = target => target.remove(),
  onEach
}: IBindProps<GElement, NewDatum, any, any>) {
  const newElems = onCreate(target.enter());
  if (onEnter) {
    onEnter(newElems, target);
  }
  if (onExit) {
    onExit(target.exit<NewDatum>());
  }
  const result = newElems.merge(target);
  onEach(result);
  return result;
}

export interface IRxBindProps<
  GElement extends Element | EnterElement,
  NewDatum,
  PElement extends BaseType,
  PDatum
> extends IBindBaseProps<GElement, NewDatum, PElement, PDatum> {
  /** A selector to recognize the target elements */
  selector: string;
}

export interface BindResult<TDatum, PElement extends BaseType> {
  /** Binds to the subscription. The type corresponds to the created element. */
  bind<GElement extends Element | EnterElement>(
    bindParams: IRxBindProps<GElement, TDatum, PElement, {}>
  ): Observable<Selection<GElement, TDatum, PElement, any>>;
}

/**
 * Binds d3 to Rx Observables.
 *
 * @param target The element to contain the bound data
 * @param data The data to bind to the elements
 * @param key The unique key for the data to identify the same elements
 */
export function rxData<TDatum, PElement extends BaseType>(
  target: Observable<Selection<PElement, any, any, any>>,
  data: Observable<TDatum[]>,
  key?: (data: TDatum, index: number) => any
): BindResult<TDatum, PElement> {
  return {
    bind: <GElement extends Element | EnterElement>({
      selector,
      ...actions
    }: IRxBindProps<GElement, TDatum, PElement, {}>) => {
      return Observable.create(
        (observer: Observer<Selection<GElement, TDatum, PElement, any>>) => {
          const onUnsubscribing = new Subject<TDatum[]>();
          const subscription = target
            .switchMap(svgSelection =>
              data.merge(onUnsubscribing).map(data =>
                svgSelection
                  .selectAll<GElement, {}>(selector)
                  .filter(function() {
                    return (this as Element).parentNode === svgSelection.node();
                  })
                  .data(data, key)
              )
            )
            .map(target =>
              bind({
                target,
                ...actions
              })
            )
            .subscribe(observer);

          return () => {
            onUnsubscribing.next([]);
            subscription.unsubscribe();
          };
        }
      ) as Observable<Selection<GElement, TDatum, PElement, any>>;
    }
  };
}

/**
 * Binds d3 to Rx Observables.
 *
 * @param data The data to bind to the elements
 */
export function rxDatum<TDatum>(data: Observable<TDatum>) {
  return <GElement extends BaseType, PElement extends BaseType, TOldDatum>(
    /** The element to contain the bound data */
    target: Observable<Selection<GElement, any, PElement, TOldDatum>>
  ): Observable<Selection<GElement, TDatum, PElement, TOldDatum>> =>
    target.switchMap(svgSelection =>
      data.map(data => svgSelection.datum(data))
    );
}

export interface IEventOccurred<GElement extends BaseType, TDatum> {
  target: GElement;
  datum: TDatum;
  index: number;
  groups: GElement[] | ArrayLike<GElement>;
  event: any;
  mouse: [number, number];
}

export type GetEventFn<T extends BaseType, Datum, Result> = (
  _this: T,
  datum: Datum,
  index: number,
  groups: T[] | ArrayLike<T>
) => Result;

function getEvent<GElement extends BaseType, TDatum>(
  _this: GElement,
  datum: TDatum,
  index: number,
  groups: GElement[] | ArrayLike<GElement>
): IEventOccurred<GElement, TDatum> {
  return {
    target: _this,
    datum,
    index,
    groups,
    event: d3event,
    mouse: d3mouse(_this as any)
  };
}

export type IEventEmitter<EventName extends string, TResult> = {
  on(eventName: EventName, fn: null | ((event: TResult) => void)): void;
};

export function fnEvent<EventName extends string, TDatum, TResult>(
  eventName: EventName,
  params: {
    toResult: (eventTarget: any, eventArg: TDatum) => TResult;
  }
): (
  target: Observable<IEventEmitter<EventName, TDatum>>
) => Observable<TResult>;
export function fnEvent<EventName extends string>(
  eventName: EventName,
  params?: {
    capture?: boolean;
  }
): <GElement extends BaseType, TDatum>(
  target: Observable<Selection<GElement, TDatum, any, any>>
) => Observable<IEventOccurred<GElement, TDatum>>;
export function fnEvent<
  EventName extends string,
  TDatum,
  GElement extends BaseType,
  TTarget extends Selection<GElement, TDatum, any, any>
>(
  eventName: EventName,
  params?: {
    capture?: boolean;
    toResult?: (target: any, eventArg: TDatum, p2: any, p3: any) => any;
  }
): (target: Observable<TTarget>) => Observable<any> {
  const { capture = undefined, toResult = getEvent } = params || {};
  return target =>
    target.switchMap(
      element =>
        new Observable<any>(observer => {
          element.on(
            eventName,
            function(datum, index, groups) {
              observer.next(toResult(this, datum, index, groups));
            },
            capture
          );

          return () => {
            element.on(eventName, null);
          };
        })
    );
}

export function fnSelect<T extends BaseType>(query: string) {
  return <TDatum, PElement extends BaseType, PDatum>(
    container: Selection<BaseType, TDatum, PElement, PDatum>
  ) => container.select<T>(query);
}
