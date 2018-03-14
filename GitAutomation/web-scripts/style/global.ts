import { cssRule, types } from "typestyle";

const fontFamily = `Arial, Helvetica, sans-serif`;

cssRule("body", {
  margin: 0,
  display: "flex",
  flexDirection: "column",
  maxHeight: "100vh",
  $nest: {
    "> div": {
      margin: 0,
      display: "flex",
      flexDirection: "column",
      maxHeight: "100vh"
    }
  }
});

cssRule("*", {
  cursor: "default",
  fontFamily
});

export const linkStyle: types.NestedCSSProperties = {
  textDecoration: "underline",
  color: serviceLineColors[3],
  cursor: "pointer",

  $nest: {
    "*": {
      cursor: "pointer"
    }
  }
};
cssRule("a:not(.normal)", linkStyle);

export const normalLinkStyle: types.NestedCSSProperties = {
  textDecoration: "none",
  color: "currentColor",
  cursor: "pointer",

  $nest: {
    "*": {
      cursor: "pointer"
    }
  }
};
cssRule("a.normal", normalLinkStyle);

cssRule("button", {
  color: "white",
  border: `1px solid ${serviceLineColors[3]}`,
  backgroundColor: serviceLineColors[3],
  fontFamily,
  fontSize: "1rem",
  padding: "0.25rem",
  borderRadius: "4px",

  $nest: {
    "&.secondary": {
      borderColor: "transparent",
      backgroundColor: "transparent",
      color: serviceLineColors[3]
    }
  }
});

export const bodyBackgroundColor = "#f4f4f9";
cssRule("body", {
  backgroundColor: bodyBackgroundColor
});

cssRule("html", {
  fontSize: "100%"
});

export const textImageStyle: types.NestedCSSProperties = {
  height: "1em"
};
cssRule(".text-image", textImageStyle);
