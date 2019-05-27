import React from "react";
import { useService } from "../../injector";
import { useSubscription } from "../../rxjs";
import { toLookup } from "../../data-manipulators";

import { FlowNode, FlowLink } from "./types";
import { FlowSimulation } from "./FlowSimulation";

function keys(o: {}): string[] {
  return Object.keys(o);
}

export function DataDrivenSimulation({
  children,
}: {
  children?: (
    nodes: FlowNode[],
    links: FlowLink[]
  ) => React.ReactElement | null;
}) {
  const api = useService("api");
  const [{ nodes, links }, setData] = React.useState<{
    nodes: FlowNode[];
    links: FlowLink[];
  }>({ nodes: [], links: [] });

  useSubscription(
    () =>
      api.reserves$.subscribe(reserveData => {
        const incomingNodes = toLookup(
          keys(reserveData),
          name => name,
          (reserveName): FlowNode => ({
            reserveName,
            reserve: reserveData[reserveName],
          })
        );
        const updatedOldNodes = toLookup(
          nodes,
          node => node.reserveName,
          node => ({ ...node, reserve: reserveData[node.reserveName] })
        );
        // The nodes get modified downstream, so we want to preserve them where we can
        const nodeLookup = { ...incomingNodes, ...updatedOldNodes };
        const newNodes = Object.values(nodeLookup).filter(node => node.reserve);
        const links = keys(reserveData).reduce<FlowLink[]>((acc, next) => {
          acc.push(
            ...keys(reserveData[next].Upstream).map<FlowLink>(up => ({
              source: nodeLookup[up],
              target: nodeLookup[next],
            }))
          );
          return acc;
        }, []);
        setData({ nodes: newNodes, links });
      }),
    [api]
  );

  return <FlowSimulation nodes={nodes} links={links} children={children} />;
}
