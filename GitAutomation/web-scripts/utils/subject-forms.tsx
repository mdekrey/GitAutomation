import * as React from "react";
import { Observable, NextObserver, Subject } from "./rxjs";

export type SubjectElement<TElem, TValue> = React.HTMLProps<TElem> & {
  subject: Subject<TValue>;
};
export type ObservableElement<TElem, TValue> = React.HTMLProps<TElem> & {
  observable: Observable<TValue>;
  observer: NextObserver<TValue>;
};

export const InputObservable = ({
  observer,
  observable,
  ...props
}: ObservableElement<HTMLInputElement, string>) =>
  observable
    .map(value => (
      <input
        {...props}
        value={value}
        onChange={ev => observer.next(ev.currentTarget.value)}
      />
    ))
    .asComponent();

export const InputSubject = ({
  subject,
  ...props
}: SubjectElement<HTMLInputElement, string>) => (
  <InputObservable {...props} observer={subject} observable={subject} />
);

export const CheckboxObservable = ({
  observer,
  observable,
  ...props
}: ObservableElement<HTMLInputElement, boolean>) =>
  observable
    .map(value => (
      <input
        type="checkbox"
        {...props}
        checked={value}
        onChange={ev => observer.next(ev.currentTarget.checked)}
      />
    ))
    .asComponent();

export const CheckboxSubject = ({
  subject,
  ...props
}: SubjectElement<HTMLInputElement, boolean>) => (
  <CheckboxObservable {...props} observer={subject} observable={subject} />
);

export const SelectObservable = ({
  observer,
  observable,
  ...props
}: ObservableElement<HTMLSelectElement, string>) =>
  observable
    .map(value => (
      <select
        {...props}
        value={value}
        onChange={ev => observer.next(ev.currentTarget.value)}
      />
    ))
    .asComponent();

export const SelectSubject = ({
  subject,
  ...props
}: SubjectElement<HTMLSelectElement, string>) => (
  <SelectObservable {...props} observable={subject} observer={subject} />
);
