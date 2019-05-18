import React from "react";
import { useService } from "../injector";
import { useObservable } from "../rxjs";
import "../api";

export function Home() {
  const api = useService("api");
  const reserveTypes = useObservable(api.reserveTypes$, undefined, [api]);
  const branches = useObservable(api.branches$, undefined, [api]);
  const reserves = useObservable(api.reserves$, undefined, [api]);

  return (
    <div>
      <h1>Hello, world!</h1>
      <h2>Reserve Types</h2>
      {reserveTypes ? (
        <dl>
          {Object.keys(reserveTypes).map(key => (
            <React.Fragment key={key}>
              <dt>{key}</dt>
              <dd>{reserveTypes[key].Description}</dd>
            </React.Fragment>
          ))}
        </dl>
      ) : (
        <p>Not available</p>
      )}
      <h2>Reserves</h2>
      {reserves ? (
        <dl>
          {Object.keys(reserves).map(key => (
            <React.Fragment key={key}>
              <dt>{key}</dt>
              <dd>{reserves[key].ReserveType}</dd>
            </React.Fragment>
          ))}
        </dl>
      ) : (
        <p>Not available</p>
      )}
      <h2>Branches</h2>
      {branches ? (
        <dl>
          {Object.keys(branches).map(key => (
            <React.Fragment key={key}>
              <dt>{key}</dt>
              <dd>{branches[key]}</dd>
            </React.Fragment>
          ))}
        </dl>
      ) : (
        <p>Not available</p>
      )}
    </div>
  );
}
