import { Observable, Subscription } from "rxjs";
import { select as d3select, Selection, BaseType } from "d3-selection";

export type IRxBindProps<
  GElement extends BaseType,
  NewDatum,
  PElement extends BaseType,
  PDatum
> = {
  /** The element to be created per datum */
  element: string;
  /** A selector to recognize the target elements */
  selector?: string;
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
};

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
  target: Observable<PElement>,
  data: Observable<TDatum[]>,
  key?: (data: TDatum) => any
): BindResult<TDatum, PElement> {
  return {
    bind: <GElement extends BaseType>({
      selector,
      element,
      onEnter,
      onExit = target => target.remove(),
      onEach
    }: IRxBindProps<GElement, TDatum, PElement, {}>) => {
      return target
        .map(elem => d3select(elem))
        .switchMap(svgSelection =>
          data.map(data =>
            svgSelection
              .selectAll<GElement, {}>(selector || element)
              .data(data, key)
          )
        )
        .subscribe(target => {
          const newElems = target.enter().append<GElement>(element);
          if (onEnter) {
            onEnter(newElems, target);
          }
          if (onExit) {
            onExit(target.exit<TDatum>());
          }
          onEach(newElems.merge(target));
        });
    }
  };
}

/**
 * Binds d3 to Rx Observables.
 *
 * @param target The element to contain the bound data
 * @param data The data to bind to the elements
 */
export function rxDatum<TDatum, PElement extends BaseType>(
  target: Observable<PElement>,
  data: Observable<TDatum>
): Observable<Selection<PElement, TDatum, null, undefined>> {
  return target
    .map(elem => d3select(elem))
    .switchMap(svgSelection => data.map(data => svgSelection.datum(data)));
}
