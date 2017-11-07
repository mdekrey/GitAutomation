import { cssRule, types } from "typestyle";
import { rgb } from "csx";

const fontFamily = `Arial, Helvetica, sans-serif`;

cssRule("*", {
  cursor: "default",
  fontFamily
});

export const linkStyle: types.NestedCSSProperties = {
  textDecoration: "underline",
  color: rgb(111, 206, 31).toString(),
  cursor: "pointer"
};
cssRule("a", linkStyle);
