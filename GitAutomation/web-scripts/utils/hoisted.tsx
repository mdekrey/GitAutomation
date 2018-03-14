import * as React from "react";
import {
  unstable_renderSubtreeIntoContainer,
  unmountComponentAtNode
} from "react-dom";

class HoistedPortal extends React.PureComponent<{}, never> {
  render() {
    return <>{this.props.children}</>;
  }
}

export class Hoisted extends React.PureComponent<{}, never> {
  private readonly node: HTMLDivElement = document.createElement("div");

  componentDidMount() {
    document.body.appendChild(this.node);
    this.renderPortal(this.props);
  }

  componentDidUpdate() {
    this.renderPortal(this.props);
  }

  componentWillUnmount() {
    unmountComponentAtNode(this.node);
    document.body.removeChild(this.node);
  }

  renderPortal(props: Hoisted["props"]) {
    unstable_renderSubtreeIntoContainer(
      this,
      <HoistedPortal {...props} />,
      this.node
    );
  }

  render() {
    return null;
  }
}
