import React from "react";

type ComponentType<TProps> = React.ComponentType<TProps>;

type FirstComponent<TProps> = ComponentType<TProps> | string;

export type ChainProps<TProps> = {
  Component:
    | (FirstComponent<TProps>)
    | [FirstComponent<TProps>, ...ComponentType<ChainProps<TProps> & TProps>[]];
};

function splitChain<T>(props: ChainProps<T> & T) {
  const { Component: components, ...otherProps } = props;
  return { components, otherProps: (otherProps as unknown) as T };
}

function coerceArray<T, U>(target: T | [T, ...U[]]) {
  return Array.isArray(target) ? target : ([target] as [T]);
}

function unrollChain<TProps extends TMyProps, TMyProps>(
  props: ChainProps<TProps> & TProps,
  adjust: (p: TMyProps) => TMyProps
) {
  const { components, otherProps } = splitChain(props);
  const [Component, ...nextComponents] = coerceArray(components);
  const Last = nextComponents.pop();
  const adjusted = { ...otherProps, ...adjust(otherProps) } as TProps;
  if (Last) {
    return <Last Component={[Component, ...nextComponents]} {...adjusted} />;
  } else {
    return <Component {...adjusted} />;
  }
}

export function Chainable<TProps>(name: string, adjust: (p: TProps) => TProps) {
  function result<TComponent extends React.Component<TProps & any>>(
    props: ChainProps<PropsOf<TComponent>> & PropsOf<TComponent>
  ): JSX.Element;
  function result<TComponent extends React.FunctionComponent<TProps & any>>(
    props: ChainProps<PropsOf<TComponent>> & PropsOf<TComponent>
  ): JSX.Element;
  function result<TComponent extends React.ComponentClass<TProps & any>>(
    props: ChainProps<PropsOf<TComponent>> & PropsOf<TComponent>
  ): JSX.Element;
  function result<TComponent extends React.ComponentType<TProps>>(
    props: ChainProps<PropsOf<TComponent>> & PropsOf<TComponent>
  ) {
    return unrollChain<PropsOf<TComponent>, TProps>(props, adjust);
  }
  (result as React.NamedExoticComponent).displayName = name;
  return result;
}
