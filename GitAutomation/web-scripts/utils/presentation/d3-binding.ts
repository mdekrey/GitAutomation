import { Observable, Subscription, Observer } from "rxjs";
import {
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
  ): Subscription;
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
      return target
        .switchMap(svgSelection =>
          data.map(data =>
            svgSelection.selectAll<GElement, {}>(selector).data(data, key)
          )
        )
        .subscribe(target =>
          bind({
            target,
            ...actions
          })
        );
    }
  };
}

/**
 * Binds d3 to Rx Observables.
 *
 * @param target The element to contain the bound data
 * @param data The data to bind to the elements
 */
export function rxDatum<
  GElement extends BaseType,
  TDatum,
  PElement extends BaseType,
  TOldDatum
>(
  target: Observable<Selection<GElement, any, PElement, TOldDatum>>,
  data: Observable<TDatum>
): Observable<Selection<GElement, TDatum, PElement, TOldDatum>> {
  return target.switchMap(svgSelection =>
    data.map(data => svgSelection.datum(data))
  );
}

export interface IEventOccurred<GElement extends BaseType, TDatum> {
  target: GElement;
  datum: TDatum;
  index: number;
  groups: GElement[] | ArrayLike<GElement>;
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
  return Observable.create(
    (observer: Observer<IEventOccurred<GElement, TDatum>>) => {
      return target.subscribe(element => {
        element.on(
          eventName,
          function(datum, index, groups) {
            observer.next({ target: this, datum, index, groups });
          },
          capture
        );
      });
    }
  ) as Observable<IEventOccurred<GElement, TDatum>>;
}
