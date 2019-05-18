import { BranchReserveBranch } from "./BranchReserveBranch";
import { UpstreamReserve } from "./UpstreamReserve";
export interface BranchReserve {
  ReserveType: string;
  FlowType: string;
  Status: string;
  Upstream: Record<string, UpstreamReserve>;
  IncludedBranches: Record<string, BranchReserveBranch>;
  OutputCommit: string;
  Meta: Record<string, any>;
}
