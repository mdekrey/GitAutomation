import React from "react";
import "./CreateReserve.css";
import { useService } from "../../injector";
import { useObservable } from "../../rxjs";
import { ReserveSelection } from "./ReserveSelection";
import { RouteComponentProps } from "react-router-dom";
import { ReserveForm, CreateReserveFormFields } from "./ReserveForm";

export function CreateReserve({ location }: RouteComponentProps) {
  const params = new URLSearchParams(location.search);
  const branch = params.get("branch");
  const [reserveForm, setReserveForm] = React.useState<CreateReserveFormFields>(
    {
      name: branch
        ? branch
            .split("/")
            .slice(1)
            .join("/")
        : "",
      type: params.get("reserveType"),
      originalBranch: branch,
      upstream: [],
      flowType: null,
    }
  );

  if (reserveForm.type === null) {
    return (
      <ReserveSelection
        onSelectReserveType={type => setReserveForm({ ...reserveForm, type })}
      />
    );
  }
  return <ReserveForm form={reserveForm} onUpdateForm={setReserveForm} />;
}
