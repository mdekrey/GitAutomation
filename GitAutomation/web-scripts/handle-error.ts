import { Observable } from "./utils/rxjs";

export const handleErrorOnce = (err: any) => {
  // TODO - need a modal framework or something
  console.error(err);
};

export const handleError = <T>(t: Observable<T>): Observable<T> =>
  t.catch(err => {
    handleErrorOnce(err);
    return handleError(t);
  });
