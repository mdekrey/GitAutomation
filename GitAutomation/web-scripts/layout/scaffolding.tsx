import * as React from "react";
import { Selection } from "d3-selection";
import { style, types } from "typestyle";
import { linkStyle } from "../style/global";
import { black } from "csx";
import { application } from "../api/basics";
import { ContextComponent } from "../utils/routing-component";

type ScaffoldingPart = Selection<HTMLElement, {}, null, undefined>;

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
    backgroundImage: `url(${require(`./menu-icon.svg?fill=${
      featureColors[0]
    }`)})`,
    backgroundSize: "contain",
    backgroundRepeat: "no-repeat",
    backgroundPosition: "center center",
    color: "transparent",
    display: "block",
    width: "1em",
    height: "1em",
    fontSize: "1.875rem"
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

export class Scaffolding extends ContextComponent<
  { menu: JSX.Element },
  never
> {
  render() {
    return (
      <>
        <header className={menuStyle.header}>
          <h1
            onClick={this.navigateHome}
            className={menuStyle.title}
            data-locator="title"
          >
            {application.map(app => <>{app.title}</>).asComponent()}
          </h1>
          <section data-locator="menu" className={menuStyle.menuContainer}>
            {this.context.injector.services.routingStrategy
              .map(s => (
                <input
                  key={s.state.componentPath + "/" + s.state.remainingPath}
                  type="checkbox"
                  id="menu-expander"
                  className={menuStyle.menuExpander}
                />
              ))
              .asComponent()}
            <label
              data-locator="menu-anchor"
              className={menuStyle.menuLink}
              htmlFor="menu-expander"
            >
              Menu
            </label>
            <label
              className={menuStyle.menuContents}
              data-locator="menu-contents"
              htmlFor="menu-expander"
            >
              {this.props.menu}
            </label>
          </section>
        </header>
        {this.context.injector.services.routingStrategy
          .map(s => (
            <section
              key={s.state.componentPath + "/" + s.state.remainingPath}
              className={menuStyle.bodyContents}
            >
              {this.props.children}
            </section>
          ))
          .asComponent()}
      </>
    );
  }

  public navigateHome() {
    window.location.href = "#/";
  }
}
