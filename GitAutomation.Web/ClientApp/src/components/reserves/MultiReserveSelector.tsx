import React from "react";
import "./CreateReserve.css";
import { useService } from "../../injector";
import { useObservable } from "../../rxjs";
import "../../api";
import { map } from "rxjs/operators";
import { Button } from "../common";

export function MultiReserveSelector({
  value,
  onChange,
}: {
  value: string[];
  onChange: (reserveNames: string[]) => void;
}) {
  const [selectingReserve, setSelectingReserve] = React.useState("");
  const api = useService("api");
  const allReserves = useObservable(
    api.reserves$.pipe(map(r => Object.keys(r))),
    undefined,
    [api]
  );
  if (allReserves && allReserves.length === 0) {
    return <>No reserves available.</>;
  }
  const reserves = (allReserves || []).filter(
    r => value.indexOf(r) === -1 || r === selectingReserve
  );
  return (
    <>
      {reserves.length ? (
        <>
          <select
            value={selectingReserve}
            onChange={e => handleSelectingReserveChange(e.currentTarget.value)}>
            <option value="">
              {value.length ? "Select another..." : "Select..."}
            </option>
            {reserves.map(b => (
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
        {value.map(entry => (
          <li key={entry}>
            {entry}{" "}
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
    const result = value.filter(v => v !== selectingReserve);
    if (newSelectingReserve) {
      result.push(newSelectingReserve);
    }
    result.sort();
    setSelectingReserve(newSelectingReserve);
    onChange(result);
  }

  function addSelectedReserve() {
    // if (value.indexOf(selectingReserve) === -1) {
    //   const result = [...value, selectingReserve];
    //   result.sort();
    //   onChange(result);
    // }
    setSelectingReserve("");
  }

  function removeReserve(reserve: string) {
    const result = value.filter(r => r !== reserve);
    onChange(result);
  }
}
