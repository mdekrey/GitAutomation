import React from "react";
import { useComponentSize } from "../hooks/useComponentSize";

import "./flow.css";
import { DataDrivenSimulation } from "./DataDrivenSimulation";
import { FlowArrow, FlowPoint } from "./FlowArrow";
import { ReserveNode } from "./ReserveNode";

export function FlowDisplay() {
  const { width, height, ref } = useComponentSize<SVGSVGElement>();

  return (
    <svg className="Flow_svg" ref={ref}>
      <g style={{ transform: `translate(${width / 2}px, ${height / 2}px)` }}>
        <DataDrivenSimulation>
          {(nodes, links) => (
            <>
              {links.map(l => (
                <FlowArrow
                  key={`${l.source.reserveName}//${l.target.reserveName}`}
                  source={l.source as FlowPoint}
                  target={l.target as FlowPoint}
                />
              ))}
              {nodes.map(n => (
                <ReserveNode key={n.reserveName} node={n} />
              ))}
            </>
          )}
        </DataDrivenSimulation>
      </g>
    </svg>
  );
}
