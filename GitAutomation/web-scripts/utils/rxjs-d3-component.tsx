import * as React from "react";
import * as ReactDOM from "react-dom";
import { Observable, Subject, Subscription } from "./rxjs";
import { Selection } from "d3-selection";
import { RoutingComponent, ContextComponent } from "./routing-component";
import { d3element } from "./presentation/d3-binding";

export interface IRxD3Props {
  do: (
    container: Observable<Selection<HTMLElement, {}, null, undefined>>
  ) => RoutingComponent<never>;
}

export class RxD3 extends ContextComponent<IRxD3Props> {
  subscription: Subscription;
  renderer = new Subject<IRxD3Props["do"]>();

  componentDidMount() {
    const host = ReactDOM.findDOMNode(this).parentNode as HTMLElement;
    const hostObservable = Observable.of(d3element(host));
    this.subscription = this.renderer
      .do(v => console.log(v))
      .map(v => v(hostObservable))
      .do(v => console.log(v))
      .switchMap(routingComponent =>
        this.context.injector.services.routingStrategy
          .do(v => console.log(v))
          .switchMap(strategy => routingComponent(strategy))
      )
      .subscribe();
    this.renderer.next(this.props.do);
  }

  componentDidUpdate() {
    this.renderer.next(this.props.do);
  }

  componentWillUnmount() {
    this.subscription.unsubscribe();
  }

  render() {
    return <div />;
  }
}
