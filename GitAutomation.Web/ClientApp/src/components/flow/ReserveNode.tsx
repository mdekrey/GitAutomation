import React from "react";
import { useService } from "../../injector";
import { useObservable } from "../../rxjs";
import { FlowNode } from "./types";
import { map } from "rxjs/operators";

import "./ReserveNode.css";

export function ReserveNode({ node }: { node: FlowNode }) {
  const api = useService("api");
  const reserveColor = useObservable<string>(
    api.reserveTypes$.pipe(
      map(types => types[node.reserve.ReserveType].Color || "000")
    ),
    "000",
    [api, node.reserve.ReserveType]
  );

  return (
    <circle
      style={{ "--reserveColor": `#${reserveColor}` } as any}
      className={`ReserveNode_node ReserveNode_node_${node.reserve.Status}`}
      cx={node.x}
      cy={node.y}
    />
  );
}
