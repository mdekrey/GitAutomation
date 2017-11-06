import { neverEver } from "../utils/never";

/** Indicates that the data is currently loading and unavailable */
export interface IProgressLoading {
  state: "loading";
}
/** Indicates that the data is currently loaded and available */
export interface IProgressLoaded<T> {
  state: "loaded";
  data: T;
}
/** Indicates that the data is currently loading but the cached version is available */
export interface IProgressUpdating<T> {
  state: "updating";
  data: T;
}
/** Indicates that the data errored and is unavailable */
export interface IProgressError {
  state: "error";
  data: Error;
}

export type Progress<T> =
  | IProgressLoading
  | IProgressLoaded<T>
  | IProgressUpdating<T>
  | IProgressError;

export function handleProgress<TIn, TOut>(handler: {
  loading: () => TOut;
  loaded: (value: IProgressLoaded<TIn>) => TOut;
  updating?: (value: IProgressUpdating<TIn>) => TOut;
  error: (value: IProgressError) => TOut;
}): (value: Progress<TIn>) => TOut {
  return value => {
    switch (value.state) {
      case "loading":
        return handler.loading();
      case "loaded":
        return handler.loaded(value);
      case "updating":
        return handler.updating ? handler.updating(value) : handler.loading();
      case "error":
        return handler.error(value);
      default:
        return neverEver(value);
    }
  };
}
