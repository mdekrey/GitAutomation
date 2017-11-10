import { Observable } from "../utils/rxjs";
import { Selection } from "d3-selection";

type StylableElement = HTMLElement | SVGElement;
type StylableSelection = Selection<StylableElement, {}, null, undefined>;

export const applyStyles = (styles: Record<string, string>) => <
  T extends StylableSelection
>(
  elem: T
) => {
  elem.selectAll(`[data-class]`).each(function(this: StylableElement) {
    const classNames = this.getAttribute("data-class");
    if (classNames !== null) {
      this.className = classNames
        .split(" ")
        .map(className => styles[className])
        .filter(Boolean)
        .join(" ");
    }
  });
};

export const classed = function(styles: Record<string, string>) {
  return function<T extends StylableSelection>(
    target: Observable<T>
  ): Observable<T> {
    return target.do(applyStyles(styles));
  };
};
