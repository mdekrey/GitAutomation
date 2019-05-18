import React from "react";
import { useService } from "../injector";
import { useObservable, useIdle, IdleState } from "../rxjs";
import "../api";
import {
  ReservesSummary,
  UnreservedBranchesSummary,
  determineUnreservedBranches,
} from "./reserves";
import "./Home.css";

export function Home() {
  const api = useService("api");
  const branches = useObservable(api.branches$, undefined, [api]);
  const reserves = useObservable(api.reserves$, undefined, [api]);
  const state = useIdle([branches, reserves]);
  if (state === IdleState.InitialIdle) {
    return null;
  } else if (state === IdleState.Loading) {
    return <h1>Loading</h1>;
  }
  return (
    <div className="Home_layout">
      <div className="card Home_first">
        <ReservesSummary reserves={reserves} />
      </div>
      <div className="card Home_second">
        <UnreservedBranchesSummary
          unreservedBranches={determineUnreservedBranches(reserves, branches)}
        />
      </div>
    </div>
  );
}
