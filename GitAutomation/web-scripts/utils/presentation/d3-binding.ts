import { Observable, Observer, Subject } from "rxjs";
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
  onEach(newElems.merge(target));
}

export interface IRxBindProps<
  GElement extends BaseType,
  NewDatum,
  PElement extends BaseType,
  PDatum
> extends IBindBaseProps<GElement, NewDatum, PElement, PDatum> {
  /** A selector to recognize the target elements */
  selector: string;
}

export interface BindResult<TDatum, PElement extends BaseType> {
  /** Binds to the subscription. The type corresponds to the created element. */
  bind<GElement extends BaseType>(
    bindParams: IRxBindProps<GElement, TDatum, PElement, {}>
  ): Observable<never>;
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
  key?: (data: TDatum) => any
): BindResult<TDatum, PElement> {
  return {
    bind: <GElement extends BaseType>({
      selector,
      ...actions
    }: IRxBindProps<GElement, TDatum, PElement, {}>) => {
      return Observable.create((observer: Observer<never>) => {
        var onUnsubscribing = new Subject<TDatum[]>();
        var subscription = target
          .switchMap(svgSelection =>
            data
              .merge(onUnsubscribing)
              .map(data =>
                svgSelection.selectAll<GElement, {}>(selector).data(data, key)
              )
          )
          .subscribe(target =>
            bind({
              target,
              ...actions
            })
          );

        return () => {
          onUnsubscribing.next([]);
          subscription.unsubscribe();
        };
      }) as Observable<never>;
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

export function rxEvent<GElement extends BaseType, TDatum>({
  target,
  capture,
  eventName
}: {
  target: Observable<Selection<GElement, TDatum, any, any>>;
  eventName: string;
  capture?: boolean;
}) {
  return target.switchMap(
    element =>
      Observable.create(
        (observer: Observer<IEventOccurred<GElement, TDatum>>) => {
          element.on(
            eventName,
            function(datum, index, groups) {
              observer.next({
                target: this,
                datum,
                index,
                groups,
                event: d3event,
                mouse: d3mouse(this as any)
              });
            },
            capture
          );

          return () => {
            element.on(eventName, null);
          };
        }
      ) as Observable<IEventOccurred<GElement, TDatum>>
  );
}

export function fnSelect<T extends BaseType>(query: string) {
  return <TDatum, PElement extends BaseType, PDatum>(
    container: Selection<BaseType, TDatum, PElement, PDatum>
  ) => container.select<T>(query);
}
