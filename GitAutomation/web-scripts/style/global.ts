import { cssRule } from "typestyle";
import { rgb } from "csx";

const fontFamily = `Arial, Helvetica, sans-serif`;

cssRule("*", {
  cursor: "default",
  fontFamily
});

cssRule("a", {
  textDecoration: "underline",
  color: rgb(111, 206, 31).toString(),
  cursor: "pointer"
});
