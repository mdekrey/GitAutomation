import React from "react";
import { useService } from "../../injector";
import { useObservable } from "../../rxjs";
import "../../api";
import { map } from "rxjs/operators";
import { Button } from "../common";
import { RevisionDiff } from "../../api";
import { DiffDisplay } from "./DiffDisplay";

export function MultiReserveSelector({
  value: currentReserves,
  onChange,
  diffData = { Reserves: [], Branches: [] },
}: {
  value: string[];
  onChange: (reserveNames: string[]) => void;
  diffData?: { Reserves: RevisionDiff[]; Branches: RevisionDiff[] };
}) {
  const [selectingReserve, setSelectingReserve] = React.useState("");
  const api = useService("api");
  const allReserves = useObservable(
    React.useMemo(() => api.reserves$.pipe(map(r => Object.keys(r))), [api]),
    undefined
  );
  if (allReserves && allReserves.length === 0) {
    return <>No reserves available.</>;
  }
  const remainingReserves = (allReserves || []).filter(
    r => currentReserves.indexOf(r) === -1 || r === selectingReserve
  );

  const basis = Math.max(
    ...[...diffData.Reserves, ...diffData.Branches].map(r =>
      Math.max(r.ahead, r.behind)
    )
  );

  return (
    <>
      {remainingReserves.length ? (
        <>
          <select
            value={selectingReserve}
            onChange={e => handleSelectingReserveChange(e.currentTarget.value)}>
            <option value="">
              {currentReserves.length ? "Select another..." : "Select..."}
            </option>
            {remainingReserves.map(b => (
              <option value={b} key={b}>
                {b}
              </option>
            ))}
          </select>

          {selectingReserve ? (
            <Button
              onClick={e => {
                addSelectedReserve();
                e.preventDefault();
              }}>
              Add
            </Button>
          ) : null}
        </>
      ) : null}
      <ul>
        {currentReserves.map(entry => (
          <li key={entry}>
            {entry}{" "}
            {diffData.Reserves.filter(r => r.name === selectingReserve).map(
              r => (
                <DiffDisplay key={r.name} {...r} basis={basis} />
              )
            )}
            {entry !== selectingReserve ? (
              <Button
                onClick={e => {
                  removeReserve(entry);
                  e.preventDefault();
                }}>
                Remove
              </Button>
            ) : null}
          </li>
        ))}
      </ul>
    </>
  );

  function handleSelectingReserveChange(newSelectingReserve: string) {
    const result = currentReserves.filter(v => v !== selectingReserve);
    if (newSelectingReserve) {
      result.push(newSelectingReserve);
    }
    result.sort();
    setSelectingReserve(newSelectingReserve);
    onChange(result);
  }

  function addSelectedReserve() {
    setSelectingReserve("");
  }

  function removeReserve(reserve: string) {
    const result = currentReserves.filter(r => r !== reserve);
    onChange(result);
  }
}
