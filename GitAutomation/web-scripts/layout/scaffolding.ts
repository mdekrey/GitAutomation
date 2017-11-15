import { Observable } from "../utils/rxjs";
import { Selection } from "d3-selection";
import { fnSelect } from "../utils/presentation/d3-binding";
import { style, types } from "typestyle";
import { linkStyle } from "../style/global";
import { classed } from "../style/style-binding";
import { black } from "csx";

type ScaffoldingPart = Selection<HTMLElement, {}, null, undefined>;

const bodyStyle = style({
  margin: 0,
  display: "flex",
  flexDirection: "column",
  maxHeight: "100vh"
});
const headerBackground: types.NestedCSSProperties = {
  backgroundColor: "#fff"
};
const headerBorder = `1px solid ${featureColors[0]}`;
const headerShadow = `3px 3px 3px ${black.fadeOut(0.5).toRGBA()}`;

const menuStyle = {
  header: style(
    {
      flexShrink: 0,
      display: "flex",
      flexDirection: "row",
      alignItems: "stretch",
      boxShadow: headerShadow,
      borderBottom: headerBorder,
      $nest: {
        "> *": {
          padding: "10px",
          margin: 0
        }
      },
      zIndex: 1
    },
    headerBackground
  ),
  title: style({
    cursor: "pointer"
  }),
  bodyContents: style({
    padding: 10,
    overflowY: "auto"
  }),
  menuContainer: style({
    position: "relative"
  }),
  menuExpander: style({
    $debugName: "menuExpander",
    display: "none"
  }),
  menuLink: style(linkStyle, {
    userSelect: "none",
    backgroundImage: `url(${require(`./menu-icon.svg?fill=${featureColors[0]}`)})`,
    backgroundSize: "contain",
    backgroundRepeat: "no-repeat",
    backgroundPosition: "center center",
    color: "transparent",
    display: "block",
    width: 30,
    height: 30,
    fontSize: 30
  }),
  menuContents: style(headerBackground, {
    boxShadow: headerShadow,
    borderLeft: headerBorder,
    borderRight: headerBorder,
    position: "absolute",
    top: "100%",
    left: 0,
    display: "block",
    whiteSpace: "nowrap",
    maxHeight: 0,
    overflow: "hidden",
    $nest: {
      [`input[type="checkbox"]:checked ~ &`]: {
        transition: "max-height 500ms",
        maxHeight: "100vh",
        borderBottom: headerBorder
      },
      "> *": {
        display: "block",
        padding: "0 5px 5px 5px"
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
    .do(elem =>
      elem.html(require("./scaffolding.layout.html")).classed(bodyStyle, true)
    )
    .let(classed(menuStyle))
    .publishReplay(1)
    .refCount()
    .map((body): ScaffoldingResult => ({
      contents: fnSelect<HTMLElement>(`[data-locator="body-contents"]`)(body),
      menu: fnSelect<HTMLElement>(`[data-locator="menu-contents"]`)(body)
    }))
    .do(({ contents }) => (contents.node()!.scrollTop = 0));
