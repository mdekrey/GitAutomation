import React from "react";
import { useService } from "../injector";
import { useObservable, useIdle, IdleState } from "../rxjs";
import "../api";
import { ReservesSummary, UnreservedBranchesSummary } from "./reserves";
import "./Home.css";
import { Card } from "./common";

export function Home() {
  const api = useService("api");
  const unreservedBranchesService = useService("unreservedBranches");
  const unreservedBranches = useObservable(
    unreservedBranchesService.unreservedBranches$,
    undefined,
    [unreservedBranchesService]
  );
  const reserves = useObservable(api.reserves$, undefined, [api]);
  const state = useIdle([unreservedBranches, reserves]);
  if (state === IdleState.InitialIdle) {
    return null;
  } else if (state === IdleState.Loading) {
    return <h1>Loading</h1>;
  }
  return (
    <div className="Home_layout">
      <Card className="Home_first">
        <ReservesSummary reserves={reserves} />
      </Card>
      <Card className="Home_second">
        <UnreservedBranchesSummary unreservedBranches={unreservedBranches} />
      </Card>
    </div>
  );
}
