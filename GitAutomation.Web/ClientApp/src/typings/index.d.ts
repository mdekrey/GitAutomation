type Omit<T, K extends keyof any> = Pick<T, Exclude<keyof T, K>>;
type PropsOf<
  TComponent extends React.ComponentType<any> | React.Component<any, any>
> = TComponent extends React.ComponentType<infer U>
  ? U
  : TComponent extends React.Component<infer U, any>
  ? U
  : never;
