import * as React from "react";
import { Selection, event as d3event } from "d3-selection";
import { style, classes } from "typestyle";
import { applyStyles } from "./style/style-binding";

const linkDisplayStyle = {
  newWindowLink: classes(
    style({
      verticalAlign: "super",
      fontSize: "0.8em",
      marginLeft: "-0.3em"
    }),
    "normal"
  )
};

function stopPropagation() {
  d3event.stopPropagation();
}

export class ExternalLink extends React.PureComponent<
  {
    url: string | null | undefined;
  },
  never
> {
  render() {
    const { url } = this.props;
    if (!url) {
      return null;
    }
    return (
      <a
        target="_blank"
        data-locator="external-link"
        className={linkDisplayStyle.newWindowLink}
        href={url}
      >
        <img
          alt="Open in New Window"
          className="text-image"
          src="./images/new-window.svg"
        />
      </a>
    );
  }
}

export function applyExternalLink(
  selection: Selection<any, string | null | undefined, any, any>
) {
  selection
    .html(require("./external-window-link.html"))
    .select(`a[data-locator="external-link"]`)
    .style("display", url => (url ? "inline" : "none"))
    .attr("href", url => url || "")
    .on("click", stopPropagation);
  applyStyles(linkDisplayStyle)(selection);
}
