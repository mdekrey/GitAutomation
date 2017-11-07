import { Observable } from "../utils/rxjs";
import { Selection } from "d3-selection";

export const classed = function(styles: Record<string, string>) {
  return function(
    target: Observable<Selection<HTMLElement, {}, null, undefined>>
  ): Observable<Selection<HTMLElement, {}, null, undefined>> {
    return target.do(elem => {
      elem.selectAll(`[data-class]`).each(function(this: HTMLElement) {
        const classNames = this.getAttribute("data-class");
        if (classNames !== null) {
          this.className = classNames
            .split(" ")
            .map(className => styles[className])
            .filter(Boolean)
            .join(" ");
        }
      });
    });
  };
};
