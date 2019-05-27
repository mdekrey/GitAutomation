import React from "react";
import "./DiffDisplay.css";

export function DiffDisplay({
  basis,
  behind,
  ahead,
  reverse = false,
}: {
  basis: number;
  behind: number;
  ahead: number;
  reverse?: boolean;
}) {
  if (reverse) {
    return <DiffDisplay basis={basis} ahead={behind} behind={ahead} />;
  }
  return (
    <div className="DiffDisplay_base" style={{ "--basis": basis } as any}>
      <span className="DiffDisplay_behindLabel">{behind}</span>
      <span className="DiffDisplay_aheadLabel">{ahead}</span>
      <div className="DiffDisplay_behind">
        <span
          className="DiffDisplay_indicator"
          style={{ "--size": behind } as any}
        />
      </div>
      <div className="DiffDisplay_ahead">
        <span
          className="DiffDisplay_indicator"
          style={{ "--size": ahead } as any}
        />
      </div>
    </div>
  );
}
