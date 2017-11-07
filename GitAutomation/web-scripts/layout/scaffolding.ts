import { Observable } from "../utils/rxjs";
import { Selection } from "d3-selection";
import { fnSelect } from "../utils/presentation/d3-binding";
import { style } from "typestyle";
import { linkStyle } from "../style/global";

type ScaffoldingPart = Selection<HTMLElement, {}, null, undefined>;

const menuExpander = style({
  $debugName: "menuExpander",
  display: "none"
});
const menuStyle = {
  menuExpander,
  menuLink: style(linkStyle),
  menuContents: style({
    display: "none",
    $nest: {
      [`.${menuExpander}:checked ~ &`]: {
        display: "block"
      }
    }
  })
};

export interface ScaffoldingResult {
  contents: ScaffoldingPart;
  menu: ScaffoldingPart;
}

const classed = function(styles: Record<string, string>) {
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

export const scaffolding = (
  body: Observable<Selection<HTMLElement, {}, null, undefined>>
) =>
  body
    .do(elem => elem.html(require("./scaffolding.layout.html")))
    .let(classed(menuStyle))
    .publishReplay(1)
    .refCount()
    .map((body): ScaffoldingResult => ({
      contents: fnSelect<HTMLElement>(`[data-locator="body-contents"]`)(body),
      menu: fnSelect<HTMLElement>(`[data-locator="menu-contents"]`)(body)
    }));
