import React from "react";
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
import { of, Subscription } from "rxjs";
import { useService } from "../../injector";

export function CreateReserve({ history, location }: RouteComponentProps) {
  const api = useService("api");
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
  const valid = useObservable(
    React.useMemo(() => isValid(reserveForm), [reserveForm]),
    false
  );
  const [submitting, setSubmitting] = React.useState(false);
  const cancellation = React.useMemo(() => new Subscription(), []);
  React.useEffect(
    () => () => {
      cancellation.unsubscribe();
    },
    [cancellation]
  );

  if (submitting) {
    return <h1>Submitting</h1>;
  }
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
      setSubmitting(true);
      cancellation.add(
        api
          .createReserve({
            Name: reserveForm.name,
            Type: reserveForm.type,
            OriginalBranch: reserveForm.originalBranch,
            Upstream: reserveForm.upstream,
            FlowType: reserveForm.flowType,
          })
          .subscribe(() => history.push("/"), () => history.push("/"))
      );
    }
  }
}
