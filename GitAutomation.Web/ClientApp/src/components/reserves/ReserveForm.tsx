import React from "react";
import { useService } from "../../injector";
import { useObservable } from "../../rxjs";
import { ReserveLabel } from "./ReserveLabel";
import { MultiReserveSelector } from "./MultiReserveSelector";
import { Button, Label } from "../common";

export interface CreateReserveFormFields {
  name: string;
  type: string | null;
  originalBranch: string | null;
  upstream: string[];
  flowType: string | null;
}

export function ReserveForm({
  form,
  onUpdateForm,
}: {
  form: CreateReserveFormFields;
  onUpdateForm: (form: CreateReserveFormFields) => void;
}) {
  const api = useService("api");
  const flowTypes = useObservable(api.flowTypes$, undefined, [api]);
  const unreservedBranchesService = useService("unreservedBranches");
  const unreservedBranches = useObservable(
    unreservedBranchesService.unreservedBranches$,
    undefined,
    [unreservedBranchesService]
  );

  if (form.flowType === null && flowTypes) {
    onUpdateForm({ ...form, flowType: flowTypes[0] });
  }
  return (
    <>
      <Label
        label="Name"
        target={
          <input
            value={form.name}
            onChange={e =>
              onUpdateForm({ ...form, name: e.currentTarget.value })
            }
          />
        }
      />
      <Label
        label="Reserve Type"
        target={
          <>
            <ReserveLabel reserveName={form.type} />
            <Button
              style={{ marginLeft: "0.5em" }}
              onClick={e => onUpdateForm({ ...form, type: null })}>
              Change
            </Button>
          </>
        }
      />
      <Label
        label="Flow Type"
        target={
          <select
            value={form.flowType || (flowTypes ? flowTypes[0] : "")}
            onChange={e =>
              onUpdateForm({ ...form, flowType: e.currentTarget.value })
            }>
            {(flowTypes || []).map(t => (
              <option value={t} key={t}>
                {t}
              </option>
            ))}
          </select>
        }
      />
      <Label
        label="Original Branch"
        target={
          <select
            value={form.originalBranch || ""}
            onChange={e =>
              onUpdateForm({ ...form, originalBranch: e.currentTarget.value })
            }>
            <option value="">Create new branch</option>
            {(unreservedBranches || []).map(b => (
              <option value={b} key={b}>
                {b}
              </option>
            ))}
          </select>
        }
      />
      <Label
        label="Upstream Reserves"
        target={
          <MultiReserveSelector
            value={form.upstream}
            onChange={upstream => onUpdateForm({ ...form, upstream })}
          />
        }
      />
    </>
  );
}
