import * as React from "react";
import { Observable } from "rxjs/Observable";
import { Subject } from "rxjs/Subject";
import { Subscription } from "rxjs/Subscription";

export interface IRxProps {
  input: Observable<JSX.Element | null>;
  onDidRender?: () => void;
}

export interface IRxState {
  current: JSX.Element | null;
}

export class Rx extends React.Component<
  IRxProps & Record<string, any>,
  IRxState
> {
  private subscription: Subscription;
  private readonly source = new Subject<Observable<JSX.Element | null>>();
  state: IRxState = { current: null };

  componentDidMount(): void {
    this.subscription = this.source
      .distinctUntilChanged()
      .switch()
      .subscribe(current => this.setState(() => ({ current })));
    this.source.next(this.props.input);
    if (this.props.onDidRender) {
      this.props.onDidRender();
    }
  }
  componentDidUpdate() {
    if (this.props.onDidRender) {
      this.props.onDidRender();
    }
  }
  componentWillReceiveProps(nextProps: IRxProps): void {
    this.source.next(nextProps.input);
  }
  componentWillUnmount(): void {
    this.subscription.unsubscribe();
  }

  render() {
    const { input, onDidRender, ...props } = this.props;
    if (!this.state.current) {
      return this.state.current;
    }
    return React.cloneElement(this.state.current, props);
  }
}

export function asComponent(
  this: Observable<JSX.Element | null>,
  onDidRender?: () => void
) {
  return <Rx input={this} onDidRender={onDidRender} />;
}

declare module "rxjs/Observable" {
  interface Observable<T> {
    asComponent: typeof asComponent;
  }
}

Observable.prototype.asComponent = asComponent;
