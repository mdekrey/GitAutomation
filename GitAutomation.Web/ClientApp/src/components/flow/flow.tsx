import React from "react";

import "./flow.css";
import { DataDrivenSimulation } from "./DataDrivenSimulation";
import { FlowArrow, FlowPoint } from "./FlowArrow";
import { ReserveNode } from "./ReserveNode";

export function FlowDisplay() {
  return (
    <svg className="Flow_svg">
      <g className="Flow_centered">
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
