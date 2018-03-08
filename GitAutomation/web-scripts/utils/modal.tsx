import * as React from "react";
import { Observable, BehaviorSubject, Subject } from "./rxjs";
import { Overlay } from "./overlay";

export interface Cancellation {
  type: "cancelled";
}
const cancellation = Object.freeze({ type: "cancelled" } as Cancellation);

export function isCancellation(t: any): t is Cancellation {
  return t === cancellation;
}

export function isNotCancellation<TOther>(
  t: TOther | Cancellation
): t is TOther {
  return t !== cancellation;
}

class ActiveModal<TInput, TOutput> {
  output = new Subject<TOutput | Cancellation>();
  isActive = new BehaviorSubject<boolean>(true);

  constructor(public readonly props: TInput) {}

  complete = (value: TOutput) => {
    this.isActive.next(false);
    this.output.next(value);
    this.output.complete();
  };

  cancel = () => {
    this.isActive.next(false);
    this.output.next(cancellation);
    this.output.complete();
  };
}

export class Modal<TInput, TOutput> {
  private current = new BehaviorSubject<ActiveModal<TInput, TOutput> | null>(
    null
  );

  constructor(
    private readonly renderModal: (
      props: TInput,
      complete: (output: TOutput) => void,
      cancel: () => void
    ) => JSX.Element
  ) {}

  Display = () => {
    return this.current
      .map(
        active =>
          active === null
            ? Observable.of(null)
            : active.isActive.map(isActive => (isActive ? active : null))
      )
      .switch()
      .map(
        active =>
          active === null ? null : (
            <Overlay onRequestClose={active.cancel}>
              {this.renderModal(active.props, active.complete, active.cancel)}
            </Overlay>
          )
      )
      .asComponent();
  };

  launch(inputs: TInput): Observable<TOutput | Cancellation> {
    if (this.current.value) {
      this.current.value.cancel();
    }

    this.current.next(new ActiveModal<TInput, TOutput>(inputs));
    return this.current.value!.output;
  }
}
