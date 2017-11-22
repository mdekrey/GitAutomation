import { cssRule, types } from "typestyle";

const fontFamily = `Arial, Helvetica, sans-serif`;

cssRule("*", {
  cursor: "default",
  fontFamily
});

export const linkStyle: types.NestedCSSProperties = {
  textDecoration: "underline",
  color: serviceLineColors[3],
  cursor: "pointer"
};
cssRule("a:not(.normal)", linkStyle);

export const normalLinkStyle: types.NestedCSSProperties = {
  textDecoration: "none",
  color: "currentColor",
  cursor: "pointer"
};
cssRule("a.normal", normalLinkStyle);

cssRule("button", {
  color: serviceLineColors[3],
  border: `1px solid ${serviceLineColors[3]}`,
  backgroundColor: "transparent",
  fontFamily,
  fontSize: "16px"
});

export const bodyBackgroundColor = "#f4f4f9";
cssRule("body", {
  backgroundColor: bodyBackgroundColor,
  fontSize: "16px"
});
