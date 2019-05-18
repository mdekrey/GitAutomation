export type GenericResolver<TServices extends {}> = {
  <TService extends keyof TServices>(service: TService): TServices[TService];
};
