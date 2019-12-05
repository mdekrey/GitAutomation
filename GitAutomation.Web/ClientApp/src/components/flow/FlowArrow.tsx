import React from "react";

import "./FlowArrow.css";

export interface FlowPoint {
  x: number;
  y: number;
}

export function FlowArrow({
  className = "",
  source,
  target,
}: {
  className?: string;
  source: FlowPoint;
  target: FlowPoint;
}) {
  const angle = Math.atan2(target.y - source.y, target.x - source.x);
  const translate = `translate(${target.x}px, ${target.y}px)`;
  const rotate = `rotate(${angle}rad)`;
  return (
    <g className={`FlowArrow_container ${className}`}>
      <line
        x1={source.x}
        y1={source.y}
        x2={target.x}
        y2={target.y}
        className="FlowArrow_line"
      />
      <path
        d="M-3,0 l-10,3 l0,-6 l10,3"
        className="FlowArrow_arrowhead"
        style={{
          transform: `${translate} ${rotate}`,
        }}
      />
    </g>
  );
}
