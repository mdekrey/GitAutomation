import React from "react";
import "./CreateReserve.css";
import { ReserveSelection } from "./ReserveSelection";
import { RouteComponentProps } from "react-router-dom";
import { ReserveForm, CreateReserveFormFields } from "./ReserveForm";
import {
  Card,
  CardContents,
  CardActionBar,
  Button,
  DisabledButton,
} from "../common";
import { useObservable } from "../../rxjs";
import { of } from "rxjs";

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
  const valid = useObservable(isValid(reserveForm), false, [reserveForm]);

  if (reserveForm.type === null) {
    return (
      <ReserveSelection
        onSelectReserveType={type => setReserveForm({ ...reserveForm, type })}
      />
    );
  }
  return (
    <Card>
      <CardContents>
        <ReserveForm form={reserveForm} onUpdateForm={setReserveForm} />
      </CardContents>
      <CardActionBar>
        {valid ? (
          <Button onClick={createReserve}>Create Reserve</Button>
        ) : (
          <DisabledButton>Create Reserve</DisabledButton>
        )}
      </CardActionBar>
    </Card>
  );

  function isValid(reserveForm: CreateReserveFormFields) {
    // TODO - check if name is valid with server
    const valid = reserveForm.name && reserveForm.flowType && reserveForm.type;
    return of(Boolean(valid));
  }

  async function createReserve() {
    const valid = await isValid(reserveForm).toPromise();
    if (valid) {
      console.log(reserveForm);
    }
  }
}
