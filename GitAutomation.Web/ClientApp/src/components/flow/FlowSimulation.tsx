import React from "react";

import {
  forceLink,
  forceSimulation,
  forceManyBody,
  forceX,
  forceY,
  forceCenter,
  Force,
} from "d3-force";
import { FlowNode, FlowLink } from "./types";

/** Forces the flow to be left-to-right, upstream-to-downstream */
function forceFlow(links: FlowLink[]) {
  interface ForceFlow extends Force<FlowNode, FlowLink> {
    links(newLinks: FlowLink[]): this;
    links(): FlowLink[];
    initialize: (newNodes: FlowNode[]) => void;
  }
  let minDistance = 20;

  const force: Partial<ForceFlow> = function(alpha: number) {
    for (const link of links) {
      const { source, target } = link;
      const dist = source.x! - target.x! + minDistance;
      if (dist > 0) {
        source.vx! -= dist;
        target.vx! += dist;
      }
    }
  };
  force.initialize = (_: FlowNode[]) => {};
  force.links = function(newLinks?: FlowLink[]) {
    return arguments.length ? ((links = newLinks!), force) : links;
  } as ForceFlow["links"];

  return force as ForceFlow;
}

export function FlowSimulation({
  nodes,
  links,
  children,
}: {
  nodes: FlowNode[];
  links: FlowLink[];
  children?: (
    nodes: FlowNode[],
    links: FlowLink[]
  ) => React.ReactElement | null;
}) {
  const flowList = React.useMemo(() => forceFlow([]), []);
  const linksList = React.useMemo(
    () =>
      forceLink<FlowNode, FlowLink>([])
        .distance(link =>
          link.target.reserve.ReserveType === "line" ? 80 : 40
        )
        .strength(1),
    []
  );
  const simulation = React.useMemo(
    () =>
      forceSimulation<FlowNode>([])
        .force("link", linksList)
        .force("charge", forceManyBody().strength(-30))
        .force("center", forceCenter(0, 0))
        .force("flow", flowList),
    [linksList]
  );
  // eslint-disable-next-line
  const [_rerenderIndicator, setTiming] = React.useState(new Date());
  React.useEffect(() => {
    simulation.on("tick.react", () => {
      setTiming(new Date());
    });
    return () => {
      simulation.on("tick.react", null);
      simulation.stop();
    };
  }, [simulation, linksList, setTiming]);

  React.useEffect(() => {
    simulation
      .nodes(nodes)
      .alpha(0.3)
      .restart();
  }, [simulation, nodes]);
  React.useEffect(() => {
    linksList.links(links);
    flowList.links(links);
    simulation.alpha(0.3).restart();
  }, [linksList, links, simulation]);

  return children ? children(simulation.nodes(), linksList.links()) : null;
}
