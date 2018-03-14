import * as React from "react";
import { classes, style } from "typestyle";

import { Hoisted } from "./hoisted";

const center = style({
  display: "flex",
  $nest: {
    "&::before": {
      content: "''",
      display: "block",
      flexGrow: 1
    },
    "&::after": {
      content: "''",
      display: "block",
      flexGrow: 2
    }
  }
});

const styles = {
  overlayShade: style({
    content: "",
    display: "block",
    zIndex: 1,
    position: "fixed",
    backgroundColor: "rgba(0,0,0,0.8)",
    top: "0",
    left: "0",
    width: "100vw",
    height: "100vh"
  }),
  overlayContainer: classes(
    center,
    style({
      position: "absolute",
      top: "1rem",
      right: "1rem",
      left: "1rem",
      bottom: "1rem",
      zIndex: 2,
      overflowY: "auto",
      display: "flex",
      flexDirection: "column",
      alignItems: "center"
    })
  ),

  overlay: style({
    boxShadow: "0 0 2rem black",
    padding: "0.5rem",
    backgroundColor: "white",
    maxWidth: "40rem"
  }),

  overlayButtonBar: style({
    display: "flex",
    flexDirection: "row-reverse",
    $nest: {
      button: {
        marginLeft: "0.5rem"
      }
    }
  })
};

export const overlayButtonBar = styles.overlayButtonBar;

export interface IOverlayProps {
  onRequestClose: () => void;
}

export class Overlay extends React.Component<IOverlayProps, never> {
  render() {
    const { children } = this.props;

    return (
      <Hoisted>
        <div className={styles.overlayShade} onClick={this.handleShadeClick} />
        <div
          className={styles.overlayContainer}
          onClick={this.handleContainerClick}
        >
          <div className={styles.overlay} onClick={this.handleOverlayClick}>
            {children}
          </div>
        </div>
      </Hoisted>
    );
  }

  handleShadeClick = (event: React.MouseEvent<HTMLDivElement>) => {
    event.stopPropagation();
    this.props.onRequestClose();
  };
  handleContainerClick = (event: React.MouseEvent<HTMLDivElement>) => {
    event.stopPropagation();
    this.props.onRequestClose();
  };
  handleOverlayClick = (event: React.MouseEvent<HTMLDivElement>) => {
    event.stopPropagation();
  };
}
