import React from "react";
import "./CreateReserve.css";
import { useService } from "../../injector";
import { useObservable, useIdle, IdleState } from "../../rxjs";
import { ReserveSelection } from "./ReserveSelection";
import { RouteComponentProps } from "react-router-dom";
import { ReserveForm, CreateReserveFormFields } from "./ReserveForm";

export function CreateReserve({ location }: RouteComponentProps) {
  const params = new URLSearchParams(location.search);
  const api = useService("api");
  const reserveTypes = useObservable(api.reserveTypes$, undefined, [api]);
  const flowTypes = useObservable(api.flowType$, undefined, [api]);
  const state = useIdle([reserveTypes, flowTypes]);
  const branch = params.get("branch");
  const [reserveForm, setReserveForm] = React.useState<CreateReserveFormFields>(
    {
      name: branch
        ? branch
            .split("/")
            .slice(1)
            .join("/")
        : "",
      type: params.get("reserveForm"),
      originalBranch: branch,
      upstream: [],
      flowType: null,
    }
  );

  if (state === IdleState.InitialIdle) {
    return null;
  } else if (state === IdleState.Loading) {
    return <h1>Loading</h1>;
  }

  if (reserveForm.type === null) {
    return (
      <ReserveSelection
        reserveTypes={reserveTypes}
        onSelectReserveType={type => setReserveForm({ ...reserveForm, type })}
      />
    );
  }
  return (
    <ReserveForm
      form={reserveForm}
      onUpdateForm={setReserveForm}
      reserveTypes={reserveTypes}
      flowTypes={flowTypes}
    />
  );
}
