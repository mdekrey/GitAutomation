import { BehaviorSubject } from "rxjs";

export const windowUrlChanged = new BehaviorSubject<null>(null);

window.onpopstate = () => {
  windowUrlChanged.next(null);
};
