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
  const reserves = (allReserves || []).filter(r => value.indexOf(r) === -1);
  return (
    <>
      {reserves.length ? (
        <>
          <select
            value={selectingReserve}
            onChange={e => setSelectingReserve(e.currentTarget.value)}>
            <option value="">Select...</option>
            {reserves.map(b => (
              <option value={b} key={b}>
                {b}
              </option>
            ))}
          </select>

          {selectingReserve ? (
            <Button onClick={addSelectedReserve}>Add</Button>
          ) : null}
        </>
      ) : null}
      <ul>
        {value.map(e => (
          <li>
            {e} <Button onClick={() => removeReserve(e)}>Remove</Button>
          </li>
        ))}
      </ul>
    </>
  );

  function addSelectedReserve() {
    if (reserves.indexOf(selectingReserve) !== -1) {
      const result = [...value, selectingReserve];
      result.sort();
      onChange(result);
    }
    setSelectingReserve("");
  }

  function removeReserve(reserve: string) {
    const result = value.filter(r => r !== reserve);
    onChange(result);
  }
}
