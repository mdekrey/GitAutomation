import { Component } from "react";
import { BehaviorSubject, Observable, Subscription } from "./rxjs";

interface AllProps<TProps, TState> {
  props: TProps;
  state: TState;
}

export abstract class ObservableComponent<TProps, TState> extends Component<
  TProps,
  TState
> {
  private readonly backing: BehaviorSubject<AllProps<TProps, TState>>;
  protected readonly all$: Observable<AllProps<TProps, TState>>;
  protected readonly prop$: Observable<TProps & { children?: JSX.Element[] }>;
  protected readonly state$: Observable<TState>;
  protected readonly unmounting: Subscription = new Subscription();

  constructor(nextProps: TProps) {
    super(nextProps);
    this.state = this.initializeState(nextProps!);

    this.backing = new BehaviorSubject<AllProps<TProps, TState>>({
      props: nextProps!,
      state: this.state
    });

    this.all$ = this.backing.asObservable();
    this.prop$ = this.backing.map(b => b.props);
    this.state$ = this.backing.map(b => b.state);
  }

  componentWillMount() {
    this.backing.next({
      props: this.props,
      state: this.state
    });
  }

  /**
   * Called to initialize the state.
   * @param nextProps The properties passed to the `super` call in the constructor.
   * @param nextContext The context passed to the `super` call in the constructor.
   */
  protected abstract initializeState(nextProps: TProps): TState;

  shouldComponentUpdate(nextProps: TProps, nextState: TState): boolean {
    this.backing.next({
      props: nextProps,
      state: nextState
    });
    return false;
  }

  componentWillUnmount() {
    this.unmounting.unsubscribe();
  }

  abstract render(): JSX.Element | null;
}

export abstract class StatelessObservableComponent<
  TProps
> extends ObservableComponent<TProps, never> {
  protected initializeState(nextProps: TProps): never {
    return ({} as any) as never;
  }
}
