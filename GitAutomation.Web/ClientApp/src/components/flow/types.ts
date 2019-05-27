import { BranchReserve } from "../../api";

import { SimulationNodeDatum, SimulationLinkDatum } from "d3-force";

export interface FlowNode extends SimulationNodeDatum {
  reserveName: string;
  reserve: BranchReserve;
}

export interface FlowLink extends SimulationLinkDatum<FlowNode> {
  source: FlowNode;
  target: FlowNode;
}
