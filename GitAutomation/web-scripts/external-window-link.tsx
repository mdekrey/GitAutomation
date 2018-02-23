import * as React from "react";
import { style, classes } from "typestyle";

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
          src={require("./images/new-window.svg")}
        />
      </a>
    );
  }
}
