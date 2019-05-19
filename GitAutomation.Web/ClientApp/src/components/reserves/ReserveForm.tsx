import React from "react";
import "./CreateReserve.css";
import { useService } from "../../injector";
import { useObservable, useIdle, IdleState } from "../../rxjs";
import { ReserveSelection } from "./ReserveSelection";
import { RouteComponentProps } from "react-router-dom";
import { ReserveConfiguration } from "../../api";
import { TextLine } from "../loading";
import { ReserveLabel } from "./ReserveLabel";
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
  reserveTypes,
  flowTypes,
}: {
  form: CreateReserveFormFields;
  onUpdateForm: (form: CreateReserveFormFields) => void;
  reserveTypes: Record<string, ReserveConfiguration> | undefined;
  flowTypes: string[] | undefined;
}) {
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
            {!reserveTypes || !form.type ? (
              <TextLine />
            ) : (
              <ReserveLabel reserveType={reserveTypes[form.type]} />
            )}
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
      <Label label="Original Branch" target="TODO - Selector" />
      <Label label="Upstream Reserves" target="TODO - Selector" />
    </>
  );
}
