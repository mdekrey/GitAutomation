import { Observable } from "../utils/rxjs";
import { Selection } from "d3-selection";
import { fnSelect } from "../utils/presentation/d3-binding";
import { style } from "typestyle";
import { linkStyle } from "../style/global";
import { classed } from "../style/style-binding";

type ScaffoldingPart = Selection<HTMLElement, {}, null, undefined>;

const menuExpander = style({
  $debugName: "menuExpander",
  display: "none"
});
const menuStyle = {
  header: style({
    display: "flex",
    flexDirection: "row",
    alignItems: "baseline",
    $nest: {
      "*": {
        marginRight: 20
      }
    }
  }),
  menuContainer: style({
    position: "relative"
  }),
  menuExpander,
  menuLink: style(linkStyle, {}),
  menuContents: style({
    position: "absolute",
    top: "100%",
    left: 0,
    display: "none",
    whiteSpace: "nowrap",
    $nest: {
      [`.${menuExpander}:checked ~ &`]: {
        display: "block"
      },
      "> *": {
        display: "block"
      }
    }
  })
};

export interface ScaffoldingResult {
  contents: ScaffoldingPart;
  menu: ScaffoldingPart;
}

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
