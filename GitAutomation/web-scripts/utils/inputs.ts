import { Selection } from "d3-selection";
import { Observable } from "./rxjs";
import { rxEvent, IEventOccurred } from "./presentation/d3-binding";

export function inputChange<
  Elem extends HTMLInputElement | HTMLSelectElement,
  T
>(
  input: Observable<Selection<Elem, T, any, any>>
): Observable<IEventOccurred<Elem, T>>;
export function inputChange<
  Elem extends HTMLInputElement | HTMLSelectElement,
  T
>(
  eventNamespace?: string
): (
  input: Observable<Selection<Elem, T, any, any>>
) => Observable<IEventOccurred<Elem, T>>;
export function inputChange<
  Elem extends HTMLInputElement | HTMLSelectElement,
  T
>(
  input: Observable<Selection<Elem, T, any, any>> | string | undefined
):
  | Observable<IEventOccurred<Elem, T>>
  | ((
      input: Observable<Selection<Elem, T, any, any>>
    ) => Observable<IEventOccurred<Elem, T>>) {
  if (typeof input === "string" || input === undefined) {
    const eventNamespace = input;
    return target =>
      rxEvent({
        target,
        eventName: `change.${eventNamespace || Math.random()}`
      }).merge(
        rxEvent({
          target,
          eventName: `input.${eventNamespace || Math.random()}`
        })
      );
  }
  return inputChange();
}

export interface IChangeConfig {
  includeInitial: boolean;
  eventNamespace?: string;
}

function elementOnChange({ includeInitial, eventNamespace }: IChangeConfig) {
  return <Elem extends HTMLInputElement | HTMLSelectElement>(
    input: Observable<Selection<Elem, any, any, any>>
  ) =>
    input
      .publishReplay(1)
      .refCount()
      .let(target =>
        target
          .let(inputChange(eventNamespace))
          .map(e => e.target)
          .let(
            e =>
              includeInitial
                ? e.merge(
                    target.filter(t => Boolean(t.node())).map(t => t.node()!)
                  )
                : e
          )
      );
}

export function inputValue(changeConfig: IChangeConfig) {
  return (
    input: Observable<
      Selection<HTMLInputElement | HTMLSelectElement, any, any, any>
    >
  ) => input.let(elementOnChange(changeConfig)).map(t => t.value);
}

export function checkboxChecked(changeConfig: IChangeConfig) {
  return (input: Observable<Selection<HTMLInputElement, any, any, any>>) =>
    input.let(elementOnChange(changeConfig)).map(t => t.checked);
}
