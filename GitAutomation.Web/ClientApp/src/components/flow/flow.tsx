import React from "react";
import { useService } from "../../injector";
import { useObservable } from "../../rxjs";

import "./flow.css";
import { DataDrivenSimulation } from "./DataDrivenSimulation";

export function FlowDisplay() {
  const api = useService("api");
  const reserveTypes = useObservable(api.reserveTypes$, undefined, [api]);

  return (
    <svg className="Flow_svg">
      <g className="Flow_centered">
        <DataDrivenSimulation>
          {(nodes, links) => (
            <>
              {links.map(l => (
                <line
                  key={`${l.source.reserveName}//${l.target.reserveName}`}
                  x1={l.source.x}
                  y1={l.source.y}
                  x2={l.target.x}
                  y2={l.target.y}
                  style={{ stroke: "rgb(0,0,0)", strokeWidth: "1" }}
                />
              ))}
              {nodes.map(n => (
                <circle
                  key={n.reserveName}
                  fill={`#${reserveTypes &&
                    reserveTypes[n.reserve.ReserveType].Color}`}
                  cx={n.x}
                  cy={n.y}
                  r={5}
                />
              ))}
            </>
          )}
        </DataDrivenSimulation>
      </g>
    </svg>
  );
}
