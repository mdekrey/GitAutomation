import React from "react";
import "./CreateReserve.css";
import { useService } from "../../injector";
import { useObservable, useIdle, IdleState } from "../../rxjs";
import { ReserveSelection } from "./ReserveSelection";

export function CreateReserve() {
  const api = useService("api");
  const reserveTypes = useObservable(api.reserveTypes$, undefined, [api]);
  const state = useIdle([reserveTypes]);
  const [reserveType, setReserveType] = React.useState();

  if (state === IdleState.InitialIdle) {
    return null;
  } else if (state === IdleState.Loading) {
    return <h1>Loading</h1>;
  }

  return (
    <>
      <ReserveSelection
        reserveTypes={reserveTypes}
        onSelectReserveType={setReserveType}
      />
    </>
  );
}
