import React from "react";
import { TextLine } from "../loading";
import { ReserveLabel } from "./ReserveLabel";
import { useService } from "../../injector";
import { useObservable } from "../../rxjs";
import { groupBy } from "../../data-manipulators";
import { Link } from "react-router-dom";

export function ReserveList() {
  const api = useService("api");
  const reserves = useObservable(api.reserves$, undefined) || {};

  const reservesKeys = Object.keys(reserves);
  const groups = groupBy(reservesKeys, current =>
    Object.keys(reserves[current].Upstream).length === 0
      ? ""
      : Object.keys(reserves[current].Upstream)
  );
  const parentReserves = Object.keys(groups);
  console.log(groups, parentReserves);
  parentReserves.sort();
  return (
    <>
      <h1>Reserves</h1>
      <p className="hint">
        A reserve sets the rules for one or more branches, <em>reserving</em>{" "}
        them for a specific purpose.
      </p>
      {reserves === undefined ? (
        <ul>
          <li>
            <TextLine />
          </li>
          <li>
            <TextLine />
          </li>
          <li>
            <TextLine />
          </li>
        </ul>
      ) : (
        <>
          {parentReserves.length === 0 ? (
            <>
              <p>No reserves.</p>
            </>
          ) : (
            <ul>
              {parentReserves.map(reserveName => (
                <li key={reserveName}>
                  {reserveName ? (
                    <Link to={`/reserves/${reserveName}`}>{reserveName}</Link>
                  ) : (
                    "No upstream"
                  )}
                  {reserveName && (
                    <>
                      &nbsp;&mdash;&nbsp;
                      <ReserveLabel
                        reserveName={reserves[reserveName].ReserveType}
                      />
                    </>
                  )}
                  <ul>
                    {groups[reserveName].map(childReserveName => (
                      <li key={childReserveName}>
                        {childReserveName}
                        &nbsp;({reserves[childReserveName].Status})
                        &nbsp;&mdash;&nbsp;
                        <ReserveLabel
                          reserveName={reserves[childReserveName].ReserveType}
                        />
                      </li>
                    ))}
                  </ul>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </>
  );
}
